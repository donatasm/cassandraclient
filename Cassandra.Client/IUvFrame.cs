using NetUv;

namespace Cassandra.Client
{
    public interface IUvFrame
    {
        UvBuffer AllocBuffer();
        UvBuffer Flush();
        void Read(UvBuffer buffer, int read);
        bool IsBodyRead { get; }
        void SeekBody();
        void Recycle();
        int Read(byte[] buf, int off, int len);
        void Write(byte[] buf, int off, int len);
    }
}