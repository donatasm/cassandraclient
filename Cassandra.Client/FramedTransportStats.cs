using System.Net;

namespace Cassandra.Client
{
    public class FramedTransportStats
    {
        public virtual void IncrementTransportOpen(IPEndPoint endPoint)
        {
        }

        public virtual void IncrementTransportClose(IPEndPoint endPoint)
        {
        }

        public virtual void IncrementTransportSendFrame(IPEndPoint endPoint)
        {
        }

        public virtual void IncrementTransportReceiveFrame(IPEndPoint endPoint)
        {
        }
    }
}