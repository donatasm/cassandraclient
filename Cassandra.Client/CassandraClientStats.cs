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
    }
}
