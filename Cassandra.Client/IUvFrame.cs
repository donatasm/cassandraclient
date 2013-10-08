using NetUv;

namespace Cassandra.Client
{
    public interface IUvFrame
    {
        UvBuffer GetBuffer();
        bool IsReadCompleted(int read, UvBuffer buffer);
        void Recycle();
        int Read(byte[] buf, int off, int len);
        void Write(byte[] buf, int off, int len);
    }
}