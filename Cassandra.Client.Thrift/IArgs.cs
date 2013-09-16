using System.Net;
using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public interface IArgs
    {
        void WriteMessage(TProtocol protocol);
        IPEndPoint EndPoint { get; }
        string Keyspace { get; }
    }
}
