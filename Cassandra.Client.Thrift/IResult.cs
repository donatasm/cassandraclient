using System;
using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public interface IResult<out TResult>
    {
        IResult<TResult> ReadMessage(TProtocol protocol);
        TResult Success { get; }
        Exception Exception { get; }
    }
}