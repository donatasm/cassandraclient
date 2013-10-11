using System.Net;

namespace Cassandra.Client
{
    public interface ITransportFactory
    {
        ITransport Create(IPEndPoint endPoint);
    }
}