using System.Net;

namespace Cassandra.Client
{
    public interface ITransportFactory
    {
        bool TryCreate(IPEndPoint endPoint, out ITransport transport);
    }
}