using System.Net;

namespace Cassandra.Client
{
    public interface ICassandraTransport
    {
        IPEndPoint EndPoint { get; }
    }
}