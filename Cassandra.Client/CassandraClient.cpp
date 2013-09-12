#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        void NotifyCompleted(uv_async_t* notifier, int status)
        {
            CassandraContextQueue^ contextQueue = CassandraContextQueue::FromPointer(notifier->data);

            CassandraContext^ context;

            while (contextQueue->TryDequeue(context))
            {
                const char* address = context->_address;
                int port = context->_port;

                CassandraTransport^ transport;
                Queue<CassandraTransport^>^ transportPool = context->_client->_transportPool;

                if (transportPool->Count > 0)
                {
                    transport = transportPool->Dequeue();
                }
                else
                {
                    transport = gcnew CassandraTransport(address, port, notifier->loop);
                }

                transport->_context = context;

                // execute request callback
                context->_input(transport->_protocol);
            }
        }


        void StopCompleted(uv_async_t* stop, int status)
        {
            uv_stop(stop->loop);
        }


        CassandraClient::CassandraClient()
        {
            _contextQueue = gcnew CassandraContextQueue();
            _transportPool = gcnew Queue<CassandraTransport^>();

            _loop = uv_loop_new();

            _notifier = new uv_async_t();
            _notifier->data = _contextQueue->ToPointer();
            uv_async_init(_loop, _notifier, NotifyCompleted);

            _stop = new uv_async_t();
            uv_async_init(_loop, _stop, StopCompleted);
        }


        CassandraClient::~CassandraClient()
        {
            uv_loop_delete(_loop);

            delete _notifier;
            delete _stop;
            delete _contextQueue;

            while (_transportPool->Count > 0)
            {
                CassandraTransport^ transport = _transportPool->Dequeue();
                transport->Close();
            }
        }


        void CassandraClient::Stop()
        {
            uv_async_send(_stop);
        }


        void CassandraClient::Send(InputProtocol^ input, OutputProtocol^ output)
        {
            if (input == nullptr)
                throw gcnew ArgumentNullException("input");

            if (output == nullptr)
                throw gcnew ArgumentNullException("output");

            CassandraContext^ context = gcnew CassandraContext(input, output, this);

            _contextQueue->Enqueue(context);

            // notify that there are requests pending
            uv_async_send(_notifier);
        }


        void CassandraClient::Run()
        {
            uv_run(_loop, UV_RUN_DEFAULT);
        }


        void CassandraClient::RunAsync()
        {
            Thread^ loopThread = gcnew Thread(gcnew ThreadStart(this, &CassandraClient::RunInternal));
            loopThread->IsBackground = true;
            loopThread->Start();
        }


        void CassandraClient::RunInternal()
        {
            this->Run();
            delete this;
        }
    }
}