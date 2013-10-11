using System.Net;
using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public sealed class SetKeyspaceArgs : Apache.Cassandra.Cassandra.set_keyspace_args, IArgs
    {
        public SetKeyspaceArgs(IPEndPoint endPoint, string keyspace)
        {
            EndPoint = endPoint;
            Keyspace = keyspace;
        }

        public void WriteMessage(TProtocol protocol)
        {
            protocol.WriteMessageBegin(new TMessage("set_keyspace", TMessageType.Call, Sequence.GetId()));
            Write(protocol);
            protocol.WriteMessageEnd();
        }

        public IPEndPoint EndPoint { get; private set; }
    }
}
