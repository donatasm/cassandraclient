using System.Net;
using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public sealed class GetSliceArgs : Apache.Cassandra.Cassandra.get_slice_args, IArgs
    {
        public GetSliceArgs(IPEndPoint endPoint, string keyspace)
        {
            EndPoint = endPoint;
            Keyspace = keyspace;
        }

        public void WriteMessage(TProtocol protocol)
        {
            protocol.WriteMessageBegin(new TMessage("get_slice", TMessageType.Call, Sequence.GetId()));
            Write(protocol);
            protocol.WriteMessageEnd();
            protocol.Transport.Flush();
        }

        public IPEndPoint EndPoint { get; private set; }

        public string Keyspace { get; private set; }
    }
}