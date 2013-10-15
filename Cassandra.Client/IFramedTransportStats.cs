using System.Net;

namespace Cassandra.Client
{
    public interface IFramedTransportStats
    {
        void IncrementTransportOpen(IPEndPoint endPoint);
        void IncrementTransportClose(IPEndPoint endPoint);
        void IncrementTransportSendFrame(IPEndPoint endPoint);
        void IncrementTransportReceiveFrame(IPEndPoint endPoint);
    }
}