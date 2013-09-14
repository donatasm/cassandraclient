using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public sealed class GetSliceArgs : Apache.Cassandra.Cassandra.get_slice_args, IArgs
    {
        public GetSliceArgs(string keyspace)
        {
            Keyspace = keyspace;
        }

        public void WriteMessage(TProtocol protocol)
        {
            protocol.WriteMessageBegin(new TMessage("get_slice", TMessageType.Call, Sequence.GetId()));
            Write(protocol);
            protocol.WriteMessageEnd();
            protocol.Transport.Flush();
        }

        public string Keyspace { get; private set; }
    }
}