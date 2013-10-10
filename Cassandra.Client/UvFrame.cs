using System;
using NetUv;
using Thrift.Transport;

namespace Cassandra.Client
{
    public sealed class UvFrame : IUvFrame
    {
        private const int HeaderLength = 4;
        private const int DefaultMaxFrameSize = 16384;
        private readonly byte[] _buffer;
        private int _position;
        private int? _bodyLength;
        private readonly int _maxFrameSize;

        public UvFrame(int maxFrameSize = DefaultMaxFrameSize)
        {
            _maxFrameSize = maxFrameSize;
            _buffer = new byte[HeaderLength + _maxFrameSize];

            Recycle();
        }

        public UvBuffer AllocBuffer()
        {
            var maxBufferAvailable = _maxFrameSize + HeaderLength - _position;
            return new UvBuffer(_buffer, _position, maxBufferAvailable);
        }

        public UvBuffer Flush()
        {
            _bodyLength = 0;

            if (_position > HeaderLength)
            {
                _bodyLength = _position - HeaderLength;
            }
            else
            {
                _position = HeaderLength;
            }

            WriteFrameHeader();

            return new UvBuffer(_buffer, 0, _position);
        }

        public void Read(UvBuffer buffer, int read)
        {
            _position += read;
        }

        private void WriteFrameHeader()
        {
            _buffer[0] = (byte)(0xFF & (_bodyLength >> 24));
            _buffer[1] = (byte)(0xFF & (_bodyLength >> 16));
            _buffer[2] = (byte)(0xFF & (_bodyLength >> 8));
            _buffer[3] = (byte)(0xFF & (_bodyLength));
        }

        public bool IsBodyRead
        {
            get
            {
                return TryReadFrameBodyLength()
                    && (_bodyLength == _position - HeaderLength);
            }
        }

        public void SeekBody()
        {
            // skip frame header bytes
            _position = HeaderLength;
        }

        public void Recycle()
        {
            _position = 0;
            _bodyLength = null;
        }

        public int Read(byte[] buf, int off, int len)
        {
            if (!IsBodyRead)
            {
                throw new InvalidOperationException("Frame read not completed.");
            }

            // ReSharper disable PossibleInvalidOperationException
            var left = _bodyLength.Value - _position + HeaderLength;
            // ReSharper restore PossibleInvalidOperationException

            if (left == 0)
            {
                return 0;
            }

            var read = len;

            if (left < len)
            {
                read = left;
            }

            Buffer.BlockCopy(_buffer, _position, buf, 0, read);

            return read;
        }

        public void Write(byte[] buf, int off, int len)
        {
            if (_position == 0)
            {
                _position = HeaderLength;
            }

            var alloc = _position + len;

            if (alloc > _maxFrameSize)
            {
                throw new TTransportException(
                    String.Format(
                        "Maximum frame size {0} exceeded.",
                        _maxFrameSize));
            }

            Buffer.BlockCopy(buf, off, _buffer, _position, len);

            _position += len;
        }

        private bool TryReadFrameBodyLength()
        {
            if (_bodyLength != null)
            {
                return true;
            }

            if (_position - HeaderLength < 0)
            {
                return false;
            }

            var bodyLength = 0;
            bodyLength |= (0xFF & _buffer[0]) << 24;
            bodyLength |= (0xFF & _buffer[1]) << 16;
            bodyLength |= (0xFF & _buffer[2]) << 8;
            bodyLength |= (0xFF & _buffer[3]);

            _bodyLength = bodyLength;

            return true;
        }
    }
}