using System;
using Cassandra.Client.Thrift;

namespace Cassandra.Client
{
    internal sealed class CassandraContext
    {
        private readonly IArgs _args;
        private readonly ResultCb _resultCb;

        private ITransportPool _transportPool;

        public CassandraContext(IArgs args, ResultCb resultCb)
        {
            _args = args;
            _resultCb = resultCb;
        }

        public IArgs Args
        {
            get { return _args; }
        }

        public ResultCb ResultCb
        {
            get { return _resultCb; }
        }

        public void SendArgs(ITransport transport)
        {
            _args.WriteMessage(transport.Protocol);

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

        public ITransportPool TransportPool
        {
            set { _transportPool = value; }
        }

        private void OpenCb(ITransport transport, Exception exception)
        {
            if (Success(transport, exception))
            {
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

                    if (_transportPool != null)
                    {
                        _transportPool.Add(transport);
                    }
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
                    if (transport != null)
                    {
                        transport.CloseCb = CloseCb;
                        transport.Close();
                    }
                }
            }

            return true;
        }
    }
}