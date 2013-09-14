using System;
using System.Collections.Generic;
using Apache.Cassandra;
using Thrift;
using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public sealed class GetSliceResult : Apache.Cassandra.Cassandra.get_slice_result, IResult<List<ColumnOrSuperColumn>>
    {
        public void ReadMessage(TProtocol protocol)
        {
            if (protocol.ReadMessageBegin().Type == TMessageType.Exception)
            {
                Exception = TApplicationException.Read(protocol);
                protocol.ReadMessageEnd();
                return;
            }

            Read(protocol);
            protocol.ReadMessageEnd();

            if (!__isset.success)
            {
                if (__isset.ire)
                {
                    Exception = Ire;
                    return;
                }

                if (__isset.ue)
                {
                    Exception = Ue;
                    return;
                }

                if (__isset.te)
                {
                    Exception = Te;
                    return;
                }

                Exception = new TApplicationException(TApplicationException.ExceptionType.MissingResult, "get_slice failed: unknown result");
            }
        }

        public Exception Exception { get; private set; }
    }
}