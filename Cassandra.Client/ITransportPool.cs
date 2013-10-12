using System.Net;

namespace Cassandra.Client
{
    public interface ITransportPool
    {
        void Add(ITransport transport);
        bool TryGet(IPEndPoint endPoint, out ITransport transport);
    }
}