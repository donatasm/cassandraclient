using System;
using Cassandra.Client.Thrift;

namespace Cassandra.Client
{
    internal sealed class CassandraContext
    {
        private readonly IArgs _args;
        private readonly ResultCb _resultCb;

        public CassandraContext(IArgs args, ResultCb resultCb)
        {
            _args = args;
            _resultCb = resultCb;
        }

        public void Send(ITransport transport)
        {
            if (!transport.IsOpen)
            {
                transport.OpenCb = OpenCb;
                transport.Open();
            }
            else
            {
                Flush(transport);
            }
        }

        private void OpenCb(ITransport transport, Exception exception)
        {
            if (Success(transport, exception))
            {
                _args.WriteMessage(transport.Protocol);
                Flush(transport);
            }
        }

        private void FlushCb(ITransport transport, Exception exception)
        {
            if (Success(transport, exception))
            {
                try
                {
                    _resultCb(transport, null);
                }
                finally
                {
                    transport.Recycle();
                }
                
            }
        }

        private static void CloseCb(ITransport transport, Exception exception)
        {
            transport.Dispose();
        }

        private void Flush(ITransport transport)
        {
            transport.FlushCb = FlushCb;
            transport.Flush();
        }

        private bool Success(ITransport transport, Exception exception)
        {
            if (exception != null)
            {
                try
                {
                    _resultCb(transport, exception);
                    return false;
                }
                finally
                {
                    transport.CloseCb = CloseCb;
                    transport.Close();
                }
            }

            return true;
        }
    }
}