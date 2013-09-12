using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public sealed class DescribeVersionArgs : Apache.Cassandra.Cassandra.describe_version_args, IArgs
    {
        public void WriteMessage(TProtocol protocol)
        {
            protocol.WriteMessageBegin(new TMessage("describe_version", TMessageType.Call, Sequence.GetId()));
            Write(protocol);
            protocol.WriteMessageEnd();
            protocol.Transport.Flush();
        }
    }
}