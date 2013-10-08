using System.Net;

namespace Cassandra.Client
{
    public interface ITransport
    {
        IPEndPoint EndPoint { get; }
        void Open();
        void Close();
        int Read(byte[] buf, int off, int len);
        void Write(byte[] buf, int off, int len);
        void Flush();
        void Recycle();
    }
}