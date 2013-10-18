using System.IO;
using NetUv;

namespace Cassandra.Client
{
    public sealed class UvFrame : IUvFrame
    {
        private const int HeaderLength = 4;
        private const int AllocBufferSize = 4096;
        private readonly byte[] _allocBuffer; // buffer used by uv
        private readonly MemoryStream _bufferStream; // stream for storing frame data
        private int? _bodyLength; // frame body length

        public UvFrame()
        {
            _allocBuffer = new byte[AllocBufferSize];
            _bufferStream = new MemoryStream(1024);

            Recycle();
        }

        public UvBuffer AllocBuffer()
        {
            return new UvBuffer(_allocBuffer, 0, _allocBuffer.Length);
        }

        public UvBuffer Flush()
        {
            _bodyLength = 0;

            var position = (int)_bufferStream.Position;

            if (position > HeaderLength)
            {
                _bodyLength = position - HeaderLength;
            }
            else
            {
                _bufferStream.Position = HeaderLength;
            }

            WriteFrameHeader();

            return new UvBuffer(_bufferStream.GetBuffer(), 0, position);
        }

        public void Read(UvBuffer buffer, int read)
        {
            _bufferStream.Write(buffer.Array, 0, read);
        }

        private void WriteFrameHeader()
        {
            var buffer = _bufferStream.GetBuffer();

            buffer[0] = (byte)(0xFF & (_bodyLength >> 24));
            buffer[1] = (byte)(0xFF & (_bodyLength >> 16));
            buffer[2] = (byte)(0xFF & (_bodyLength >> 8));
            buffer[3] = (byte)(0xFF & (_bodyLength));
        }

        public bool IsBodyRead
        {
            get
            {
                return TryReadFrameBodyLength()
                    && (_bodyLength == _bufferStream.Position - HeaderLength);
            }
        }

        public void SeekBody()
        {
            // skip frame header bytes
            _bufferStream.Position = HeaderLength;
        }

        public void Recycle()
        {
            _bufferStream.Position = 0;
            _bodyLength = null;
        }

        public int Read(byte[] buf, int off, int len)
        {
            return _bufferStream.Read(buf, off, len);
        }

        public void Write(byte[] buf, int off, int len)
        {
            if (_bufferStream.Position == 0)
            {
                _bufferStream.Position = HeaderLength;
            }

            _bufferStream.Write(buf, off, len);
        }

        private bool TryReadFrameBodyLength()
        {
            if (_bodyLength != null)
            {
                return true;
            }

            if (_bufferStream.Position - HeaderLength < 0)
            {
                return false;
            }

            var bodyLength = 0;
            var buffer = _bufferStream.GetBuffer();
            bodyLength |= (0xFF & buffer[0]) << 24;
            bodyLength |= (0xFF & buffer[1]) << 16;
            bodyLength |= (0xFF & buffer[2]) << 8;
            bodyLength |= (0xFF & buffer[3]);

            _bodyLength = bodyLength;

            return true;
        }
    }
}