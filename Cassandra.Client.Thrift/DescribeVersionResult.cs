using System;
using Thrift;
using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public sealed class DescribeVersionResult : Apache.Cassandra.Cassandra.describe_version_result, IResult<string>
    {
        public IResult<string> ReadMessage(TProtocol protocol)
        {
            if (protocol.ReadMessageBegin().Type == TMessageType.Exception)
            {
                Exception = TApplicationException.Read(protocol);
                protocol.ReadMessageEnd();
                return this;
            }

            Read(protocol);
            protocol.ReadMessageEnd();

            if (!__isset.success)
            {
                Exception = new TApplicationException(TApplicationException.ExceptionType.MissingResult, "describe_version failed: unknown result");
            }

            return this;
        }

        public Exception Exception { get; private set; }
    }
}