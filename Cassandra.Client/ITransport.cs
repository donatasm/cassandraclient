using System;
using System.Net;
using Thrift.Protocol;

namespace Cassandra.Client
{
    public interface ITransport : IDisposable
    {
        void Open();
        void Close();
        void Flush();
        ResultCb OpenCb { set; }
        ResultCb CloseCb { set; }
        ResultCb FlushCb { set; }
        IPEndPoint EndPoint { get; }
        bool IsOpen { get; }
        TProtocol Protocol { get; }
        string Keyspace { get; set; }
        int Read(byte[] buf, int off, int len);
        void Write(byte[] buf, int off, int len);
        void Recycle();
    }
}