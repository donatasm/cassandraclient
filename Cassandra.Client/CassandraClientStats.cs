using System.Net;

namespace Cassandra.Client
{
    public class CassandraClientStats
    {
        public virtual void IncrementArgsEnqueued()
        {
        }

        public virtual void IncrementArgsDequeued()
        {
        }

        public virtual void IncrementTransportOpen(IPEndPoint endPoint)
        {
        }

        public virtual void IncrementTransportClose(IPEndPoint endPoint)
        {
        }

        public virtual void IncrementTransportRecycle(IPEndPoint endPoint)
        {
        }

        public virtual void IncrementTransportSendFrame(IPEndPoint endPoint)
        {
        }

        public virtual void IncrementTransportReceiveFrame(IPEndPoint endPoint)
        {
        }

        public virtual void IncrementTransportError(IPEndPoint endPoint)
        {
        }
    }
}
