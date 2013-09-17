using System;
using System.Collections.Generic;
using Apache.Cassandra;
using Thrift;
using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public sealed class DescribeRingResult : Apache.Cassandra.Cassandra.describe_ring_result, IResult<List<TokenRange>>
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

                Exception = new TApplicationException(TApplicationException.ExceptionType.MissingResult, "describe_ring failed: unknown result");
            }
        }

        public Exception Exception { get; private set; }
    }
}