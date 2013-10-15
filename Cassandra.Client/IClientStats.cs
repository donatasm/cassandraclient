namespace Cassandra.Client
{
    public interface IClientStats : IFramedTransportStats
    {
        void IncrementArgsEnqueued();
        void IncrementArgsDequeued();
    }
}