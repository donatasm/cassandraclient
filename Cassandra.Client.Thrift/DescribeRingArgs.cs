using System.Net;
using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public sealed class DescribeRingArgs : Apache.Cassandra.Cassandra.describe_ring_args, IArgs
    {
        public DescribeRingArgs(IPEndPoint endPoint, string keyspace)
        {
            EndPoint = endPoint;
            Keyspace = keyspace;
        }

        public void WriteMessage(TProtocol protocol)
        {
            protocol.WriteMessageBegin(new TMessage("describe_ring", TMessageType.Call, Sequence.GetId()));
            Write(protocol);
            protocol.WriteMessageEnd();
        }

        public IPEndPoint EndPoint { get; private set; }
    }
}