using System.Net;

namespace Cassandra.Client
{
    public interface IFramedTransportStats
    {
        void IncrementTransportOpen(EndPoint endPoint);
        void IncrementTransportClose(EndPoint endPoint);
        void IncrementTransportSendFrame(EndPoint endPoint);
        void IncrementTransportReceiveFrame(EndPoint endPoint);
    }
}