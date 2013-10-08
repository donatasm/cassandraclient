using System.Net;

namespace Cassandra.Client
{
    public class CassandraClientStats : FramedTransportStats
    {
        public virtual void IncrementArgsEnqueued()
        {
        }

        public virtual void IncrementArgsDequeued()
        {
        }

        public virtual void IncrementTransportRecycle(IPEndPoint endPoint)
        {
        }
    }
}
