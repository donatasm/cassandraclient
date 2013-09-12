using System;
using Thrift;
using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public sealed class SetKeyspaceResult : Apache.Cassandra.Cassandra.set_keyspace_result, IResult<object>
    {
        public IResult<object> ReadMessage(TProtocol protocol)
        {
            Success = null;

            if (protocol.ReadMessageBegin().Type == TMessageType.Exception)
            {
                Exception = TApplicationException.Read(protocol);
                protocol.ReadMessageEnd();
                return this;
            }

            Read(protocol);
            protocol.ReadMessageEnd();

            if (__isset.ire)
            {
                Exception = Ire;
            }

            return this;
        }

        public object Success { get; private set; }
        public Exception Exception { get; private set; }
    }
}
