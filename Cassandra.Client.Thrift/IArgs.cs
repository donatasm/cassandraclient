using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public interface IArgs
    {
        void WriteMessage(TProtocol protocol);
    }
}
