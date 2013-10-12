using System;
using System.Diagnostics;
using System.Net;
using NetUv;
using Thrift.Protocol;
using Thrift.Transport;

namespace Cassandra.Client
{
    public sealed class UvFramedTransport : TTransport, ITransport
    {
        private ResultCb _openCb;
        private ResultCb _closeCb;
        private ResultCb _flushCb;
        private IUvTcp _uvTcp;
        private bool _isOpen;
        private FramedTransportStats _stats;
        private IUvFrame _frame;
        private readonly IPEndPoint _endPoint;
        private readonly TProtocol _protocol;

        private UvFramedTransport(IPEndPoint endPoint)
        {
            _isOpen = false;
            _openCb = DefaultOpenCb;
            _closeCb = DefaultCloseCb;
            _flushCb = DefaultFlushCb;
            _endPoint = endPoint;
            _protocol = new TBinaryProtocol(this);
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

        public TProtocol Protocol
        {
            get { return _protocol; }
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
            var buffer = _frame.Flush();

            _uvTcp.Write(buffer, (tcp, exception) =>
                {
                    if (exception != null)
                    {
                        _flushCb(this, exception);
                        return;
                    }

                    // prepare frame for read
                    _frame.Recycle();

                    // receive frame
                    ReceiveFrame(tcp);
                });
        }

        private void ReceiveFrame(IUvStream tcp)
        {
            tcp.ReadStart(size => _frame.AllocBuffer(), ReceiveFrameCb);
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

            _frame.Read(buffer, read);

            if (_frame.IsBodyRead)
            {
                _uvTcp.ReadStop();
                _frame.SeekBody();
                _flushCb(this, null);
            }
        }

        public override bool IsOpen
        {
            get { return _isOpen; }
        }

        public IPEndPoint EndPoint
        {
            get { return _endPoint; }
        }

        public ResultCb OpenCb
        {
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                _openCb = value;
            }
        }

        public ResultCb CloseCb
        {
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                _closeCb = value;
            }
        }

        public ResultCb FlushCb
        {
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                _flushCb = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
        }

        public sealed class Factory : ITransportFactory
        {
            private Func<IUvTcp> _uvTcpFactory;
            private FramedTransportStats _stats;
            private IUvFrame _frame;

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

            public ITransport Create(IPEndPoint endPoint)
            {
                return new UvFramedTransport(endPoint)
                    {
                        _uvTcp = (_uvTcpFactory ?? DefaultUvTcpFactory)(),
                        _stats = _stats ?? DefaultStats,
                        _frame = _frame ?? new UvFrame()
                    };
            }

            private static readonly FramedTransportStats DefaultStats = new FramedTransportStats();

            private static IUvTcp DefaultUvTcpFactory()
            {
                return UvLoop.Default.InitUvTcp();
            }
        }

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