using System;
using System.Diagnostics;
using System.Net;
using NetUv;
using Thrift.Transport;

namespace Cassandra.Client
{
    public delegate void UvFramedTransportCb(ITransport transport, Exception exception);

    public sealed class UvFramedTransport : TTransport, ITransport
    {
        private UvFramedTransportCb _openCb;
        private UvFramedTransportCb _closeCb;
        private UvFramedTransportCb _flushCb;
        private IUvTcp _uvTcp;
        private bool _isOpen;
        private FramedTransportStats _stats;
        private IUvFrame _frame;

        private UvFramedTransport()
        {
            _isOpen = false;
        }

        public override void Open()
        {
            if (_isOpen)
            {
                throw new TTransportException("Transport already opened.");
            }

            _uvTcp.Connect(EndPoint.Address.ToString(), EndPoint.Port,
                (tcp, exception) =>
                    {
                        _isOpen = true;
                        _stats.IncrementTransportOpen(EndPoint);
                        _openCb(this, exception);
                    });
        }

        public override void Close()
        {
            _uvTcp.Close(tcp =>
                {
                    _isOpen = false;
                    _stats.IncrementTransportClose(EndPoint);
                    _closeCb(this, null);
                });
        }

        public override int Read(byte[] buf, int off, int len)
        {
            return _frame.Read(buf, off, len);
        }

        public override void Write(byte[] buf, int off, int len)
        {
            _frame.Write(buf, off, len);
        }

        public override void Flush()
        {
            if (!_isOpen)
            {
                throw new TTransportException("Transport is not opened.");
            }

            SendFrame();
        }

        public void Recycle()
        {
            _frame.Recycle();
        }

        private void SendFrame()
        {
            var buffer = _frame.GetBuffer();

            _uvTcp.Write(buffer, (tcp, exception) =>
                {
                    if (exception != null)
                    {
                        _flushCb(this, exception);
                        return;
                    }

                    ReceiveFrame(tcp);
                });
        }

        private void ReceiveFrame(IUvStream tcp)
        {
            tcp.ReadStart(size => _frame.GetBuffer(), ReceiveFrameCb);
        }

        private void ReceiveFrameCb(IUvStream tcp, int read, UvBuffer buffer)
        {
            if (read == UvStream.EOF)
            {
                _flushCb(this, new TTransportException("Remote side has closed."));
                return;
            }

            if (read < 0)
            {
                // TODO: get loop last error here
                _flushCb(this, new TTransportException(String.Format("Transport read error: {0}", read)));
                return;
            }

            if (_frame.IsReadCompleted(read, buffer))
            {
                _uvTcp.ReadStop();
                _flushCb(this, null);
            }
        }

        public override bool IsOpen
        {
            get { return _isOpen; }
        }

        public IPEndPoint EndPoint { get; private set; }

        protected override void Dispose(bool disposing)
        {
        }

        public sealed class Factory
        {
            private IPEndPoint _endPoint;
            private UvFramedTransportCb _openCb;
            private UvFramedTransportCb _flushCb;
            private UvFramedTransportCb _closeCb;
            private Func<IUvTcp> _uvTcpFactory;
            private FramedTransportStats _stats;
            private IUvFrame _frame;

            public void SetIpEndPoint(string ip, int port)
            {
                _endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            }

            public void SetOpenCb(UvFramedTransportCb openCb)
            {
                _openCb = openCb;
            }

            public void SetCloseCb(UvFramedTransportCb closeCb)
            {
                _closeCb = closeCb;
            }

            public void SetFlushCb(UvFramedTransportCb flushCb)
            {
                _flushCb = flushCb;
            }

            public void SetUvTcpFactory(Func<IUvTcp> uvTcpFactory)
            {
                _uvTcpFactory = uvTcpFactory;
            }

            public void SetStats(FramedTransportStats stats)
            {
                _stats = stats;
            }

            public void SetFrameReaderWriter(IUvFrame frame)
            {
                _frame = frame;
            }

            public UvFramedTransport Build()
            {
                return new UvFramedTransport
                    {
                        EndPoint = _endPoint ?? DefaultEndPoint,
                        _openCb = _openCb ?? DefaultOpenCb,
                        _closeCb = _closeCb ?? DefaultCloseCb,
                        _flushCb = _flushCb ?? DefaultFlushCb,
                        _uvTcp = (_uvTcpFactory ?? DefaultUvTcpFactory)(),
                        _stats = _stats ?? DefaultStats,
                        _frame = _frame ?? new UvFrame()
                    };
            }

            private static readonly FramedTransportStats DefaultStats = new FramedTransportStats();
            private static readonly IPEndPoint DefaultEndPoint = new IPEndPoint(IPAddress.Loopback, 9160);

            private static void DefaultOpenCb(ITransport transport, Exception exception)
            {
                DebugMessage("Open", transport, exception);
            }

            private static void DefaultCloseCb(ITransport transport, Exception exception)
            {
                DebugMessage("Close", transport, exception);
            }

            private static void DefaultFlushCb(ITransport transport, Exception exception)
            {
                DebugMessage("Flush", transport, exception);
            }

            private static IUvTcp DefaultUvTcpFactory()
            {
                return UvLoop.Default.InitUvTcp();
            }

            [Conditional("DEBUG")]
            private static void DebugMessage(string callback, ITransport transport, Exception exception)
            {
                Debug.WriteLine("{0} callback: endpoint={1}, exception: {2}",
                    callback,
                    transport.EndPoint,
                    exception == null ? "<null>" : exception.Message);
            }
        }
    }
}