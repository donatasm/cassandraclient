using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public sealed class SetKeyspaceArgs : Apache.Cassandra.Cassandra.set_keyspace_args, IArgs
    {
        public void WriteMessage(TProtocol protocol)
        {
            protocol.WriteMessageBegin(new TMessage("set_keyspace", TMessageType.Call, Sequence.GetId()));
            Write(protocol);
            protocol.WriteMessageEnd();
            protocol.Transport.Flush();
        }
    }
}
