#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        TTransportException^ MaximumFrameSizeExceeded()
        {
            return gcnew TTransportException(String::Format("Maximum frame size {0} exceeded.", MAX_FRAME_SIZE));
        }


        void OpenCompleted(uv_connect_t* connectRequest, int status)
        {
            CassandraTransport^ transport = CassandraTransport::FromPointer(connectRequest->data);
            delete connectRequest;

            if (status != 0)
            {
                transport->_context->SetError(status);
                return;
            }

            transport->_isOpen = true;
            transport->SendFrame();
        }


        void SendFrameCompleted(uv_write_t* writeRequest, int status)
        {
            CassandraTransport^ transport = CassandraTransport::FromPointer(writeRequest->data);
            delete writeRequest;

            if (status != 0)
            {
                transport->_context->SetError(status);
                return;
            }

            transport->_socketBuffer->socket.data = transport->ToPointer();

            transport->ReceiveFrame();
        }


        void ReceiveFrameCompleted(uv_stream_t* socket, ssize_t nread, const uv_buf_t* buffer)
        {
            CassandraTransport^ transport = CassandraTransport::FromPointer(socket->data);

            if (nread < 0)
            {
                transport->_context->SetError((int)nread);
                return;
            }

            if (transport->_position + nread > MAX_FRAME_SIZE)
            {
                transport->_context->SetError(MaximumFrameSizeExceeded());
                return;
            }

            // read frame header
            if (transport->_position < FRAME_HEADER_SIZE)
            {
                int header = 0;

                if (nread > 0)
                {
                    header |= (buffer->base[0] & 0xFF) << 24;
                }
                if (nread > 1)
                {
                    header |= (buffer->base[1] & 0xFF) << 16;
                }
                if (nread > 2)
                {
                    header |= (buffer->base[2] & 0xFF) << 8;
                }
                if (nread > 3)
                {
                    header |= (buffer->base[3] & 0xFF);
                }

                transport->_header = header;
            }

            transport->_position += (int)nread;

            // check if frame already received
            if (transport->_header == transport->_position - FRAME_HEADER_SIZE)
            {
                // stop reading
                int error = uv_read_stop((uv_stream_t*)&transport->_socketBuffer->socket);
                if (error != 0)
                {
                    transport->_context->SetError(error);
                    return;
                }

                transport->_position = FRAME_HEADER_SIZE;
                transport->_context->_resultCallback(transport->_protocol, nullptr);
            }
        }


        void CloseCompleted(uv_handle_t* socket)
        {
            SocketBuffer* socketBuffer = (SocketBuffer*)socket->data;

            delete socketBuffer;
        }


        void AllocateFrameBuffer(uv_handle_t* socket, size_t size, uv_buf_t* buffer)
        {
            CassandraTransport^ transport = CassandraTransport::FromPointer(socket->data);
            int position = transport->_position;

            buffer->base = transport->_socketBuffer->buffer + position;
            buffer->len = MAX_FRAME_SIZE - position;
        }


        CassandraTransport::CassandraTransport(IPEndPoint^ endPoint, CassandraTransport::Factory^ factory, uv_loop_t* loop)
        {
            _handle = GCHandle::Alloc(this);

            _endPoint = endPoint;
            _factory = factory;
            _protocol = gcnew TBinaryProtocol(this);

            _loop = loop;
            _address = static_cast<char*>(Marshal::StringToHGlobalAnsi(endPoint->Address->ToString()).ToPointer());
            _port = endPoint->Port;
            _position = FRAME_HEADER_SIZE; // reserve first bytes for frame header
            _header = 0;
            _isOpen = false;
            _socketBuffer = new SocketBuffer();
        }


        CassandraTransport::~CassandraTransport()
        {
            Close();
        }


        void CassandraTransport::Open()
        {
            _context->_client->_stats->IncrementTransportOpen(_context->_args->EndPoint);

            struct sockaddr_in address;

            int error;

            error = uv_ip4_addr(_address, _port, &address);
            if (error != 0)
            {
                _context->SetError(error);
                return;
            }

            error = uv_tcp_init(_loop, &_socketBuffer->socket);
            if (error != 0)
            {
                _context->SetError(error);
                return;
            }

            uv_connect_t* connectRequest = new uv_connect_t();
            connectRequest->data = this->ToPointer();

            error = uv_tcp_connect(connectRequest, &_socketBuffer->socket, (const sockaddr*)&address, OpenCompleted);
            if (error != 0)
            {
                delete connectRequest;
                _context->SetError(error);
                return;
            }
        }


        void CassandraTransport::Close()
        {
            _context->_client->_stats->IncrementTransportClose(_endPoint);

            _isOpen = false;

            if (_handle.IsAllocated)
            {
                _handle.Free();
            }

            _socketBuffer->socket.data = _socketBuffer;

            uv_close((uv_handle_t*)&_socketBuffer->socket, CloseCompleted);

            _factory->CloseTransport(_endPoint);
        }


        bool CassandraTransport::IsOpen::get()
        {
            return _isOpen;
        }


        int CassandraTransport::Read(array<byte>^ buf, int off, int len)
        {
            int left = _header - _position + FRAME_HEADER_SIZE;
            int read;

            if (left < len)
            {
                read = left;
            }
            else
            {
                read = len;
            }

            if (read > 0)
            {
                IntPtr source = IntPtr::IntPtr(_socketBuffer->buffer) + _position;

                Marshal::Copy(source, buf, off, read);

                _position += read;

                return read;
            }

            return 0;
        }


        void CassandraTransport::Write(array<byte>^ buf, int off, int len)
        {
            if (_position + len > MAX_FRAME_SIZE)
            {
                _context->SetError(MaximumFrameSizeExceeded());
                return;
            }

            IntPtr destination = IntPtr::IntPtr(_socketBuffer->buffer) + _position;

            Marshal::Copy(buf, off, destination, len);
            
            _position += len;
        }


        void CassandraTransport::Flush()
        {
            if (!_isOpen)
            {
                Open();
            }
            else
            {
                SendFrame();
            }
        }


        void CassandraTransport::PrepareWrite()
        {
            _position = FRAME_HEADER_SIZE;
            _header = 0;
        }


        void CassandraTransport::Recycle()
        {
            _context->_client->_stats->IncrementTransportRecycle(_endPoint);

            // prepare for the next frame to be written
            PrepareWrite();

            // return to the pool
            _context->_client->_transportPool->Add(this);
        }


        IPEndPoint^ CassandraTransport::EndPoint::get()
        {
            return _endPoint;
        }


        void* CassandraTransport::ToPointer()
        {
            return GCHandle::ToIntPtr(_handle).ToPointer();
        }


        CassandraTransport^ CassandraTransport::FromPointer(void* ptr)
        {
            GCHandle handle = GCHandle::FromIntPtr(IntPtr(ptr));
            return (CassandraTransport^)handle.Target;
        }


        void CassandraTransport::SendFrame()
        {
            _context->_client->_stats->IncrementTransportSendFrame(_endPoint);

            _header = _position - FRAME_HEADER_SIZE;

            _socketBuffer->buffer[0] = (0xFF & (_header >> 24));
            _socketBuffer->buffer[1] = (0xFF & (_header >> 16));
            _socketBuffer->buffer[2] = (0xFF & (_header >> 8));
            _socketBuffer->buffer[3] = (0xFF & (_header));

            uv_write_t* writeRequest = new uv_write_t();
            writeRequest->data = this->ToPointer();

            uv_buf_t buffer;
            buffer.base = _socketBuffer->buffer;
            buffer.len = _position;

            int error = uv_write(writeRequest, (uv_stream_t*)&_socketBuffer->socket, &buffer, 1, SendFrameCompleted);
            if (error != 0)
            {
                delete writeRequest;
                _context->SetError(error);
                return;
            }
        }


        void CassandraTransport::ReceiveFrame()
        {
            _context->_client->_stats->IncrementTransportReceiveFrame(_endPoint);

            _position = 0;
            _header = 0;

            int error = uv_read_start((uv_stream_t*)&_socketBuffer->socket, AllocateFrameBuffer, ReceiveFrameCompleted);
            if (error != 0)
            {
                _context->SetError(error);
                return;
            }
        }


        CassandraTransport::Factory::Factory(int maxEndPointTransportCount, uv_loop_t* loop)
        {
            _maxEndPointTransportCount = maxEndPointTransportCount;
            _loop = loop;
            _endPointTransportCount = gcnew Dictionary<IPEndPoint^, int>();
        }


        CassandraTransport^ CassandraTransport::Factory::CreateTransport(IPEndPoint^ endPoint)
        {
            int endPointTransportCount;

            if (!_endPointTransportCount->TryGetValue(endPoint, endPointTransportCount))
            {
                _endPointTransportCount->Add(endPoint, 0);
            }

            if (endPointTransportCount == _maxEndPointTransportCount)
            {
                return nullptr;
            }

            _endPointTransportCount[endPoint] = endPointTransportCount + 1;

            return gcnew CassandraTransport(endPoint, this, _loop);
        }


        void CassandraTransport::Factory::CloseTransport(IPEndPoint^ endPoint)
        {
            _endPointTransportCount[endPoint] = _endPointTransportCount[endPoint] - 1;
        }
    }
}