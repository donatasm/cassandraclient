using System;
using Thrift.Protocol;

namespace Cassandra.Client.Thrift
{
    public interface IResult
    {
        void ReadMessage(TProtocol protocol);
    }

    public interface IResult<out TResult> : IResult
    {
        TResult Success { get; }
        Exception Exception { get; }
    }
}