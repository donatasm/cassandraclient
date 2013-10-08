using System;
using NetUv;

namespace Cassandra.Client
{
    public sealed class UvFrame : IUvFrame
    {
        private const int DefaultMaxFrameSize = 16384;
        private readonly byte[] _buffer;
        private int _position;
        private int _length;

        public UvFrame(int maxFrameSize = DefaultMaxFrameSize)
        {
            _buffer = new byte[maxFrameSize];
            Recycle();
        }

        public UvBuffer GetBuffer()
        {
            return new UvBuffer(_buffer, _position, _length);
        }

        public bool IsReadCompleted(int read, UvBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public void Recycle()
        {
            _position = 0;
            _length = 0;
        }

        public int Read(byte[] buf, int off, int len)
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] buf, int off, int len)
        {
            throw new NotImplementedException();
        }
    }
}