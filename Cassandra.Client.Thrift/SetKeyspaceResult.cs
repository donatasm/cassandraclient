using System;
using Thrift;
using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public sealed class SetKeyspaceResult : Apache.Cassandra.Cassandra.set_keyspace_result, IResult<object>
    {
        public void ReadMessage(TProtocol protocol)
        {
            Success = null;

            if (protocol.ReadMessageBegin().Type == TMessageType.Exception)
            {
                Exception = TApplicationException.Read(protocol);
                protocol.ReadMessageEnd();
                return;
            }

            Read(protocol);
            protocol.ReadMessageEnd();

            if (__isset.ire)
            {
                Exception = Ire;
            }
        }

        public object Success { get; private set; }
        public Exception Exception { get; private set; }
    }
}
