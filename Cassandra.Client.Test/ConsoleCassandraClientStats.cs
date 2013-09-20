using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Cassandra.Client.Test
{
    internal sealed class ConsoleCassandraClientStats : CassandraClientStats
    {
        private long _argsEnqueued;
        private readonly Dictionary<IPEndPoint, EndPointCounter> _endPointCounters;

        public ConsoleCassandraClientStats()
        {
            _argsEnqueued = 0;
            _endPointCounters = new Dictionary<IPEndPoint, EndPointCounter>();
            ArgsDequeued = 0;
        }

        public override void IncrementArgsEnqueued()
        {
            Interlocked.Increment(ref _argsEnqueued);
        }

        public override void IncrementArgsDequeued()
        {
            ArgsDequeued++;
        }

        public override void IncrementTransportOpen(IPEndPoint endPoint)
        {
            AddIfNotExists(endPoint);
            _endPointCounters[endPoint].IncrementOpenCount();
        }

        public override void IncrementTransportClose(IPEndPoint endPoint)
        {
            AddIfNotExists(endPoint);
            _endPointCounters[endPoint].IncrementCloseCount();
        }

        public override void IncrementTransportRecycle(IPEndPoint endPoint)
        {
            AddIfNotExists(endPoint);
            _endPointCounters[endPoint].IncrementRecycleCount();
        }

        public override void IncrementTransportSendFrame(IPEndPoint endPoint)
        {
            AddIfNotExists(endPoint);
            _endPointCounters[endPoint].IncrementSendFrameCount();
        }

        public override void IncrementTransportReceiveFrame(IPEndPoint endPoint)
        {
            AddIfNotExists(endPoint);
            _endPointCounters[endPoint].IncrementReceiveFrameCount();
        }

        public override void IncrementTransportError(IPEndPoint endPoint)
        {
            AddIfNotExists(endPoint);
            _endPointCounters[endPoint].IncrementErrorCount();
        }

        private void AddIfNotExists(IPEndPoint endPoint)
        {
            if (!_endPointCounters.ContainsKey(endPoint))
            {
                _endPointCounters.Add(endPoint, new EndPointCounter(endPoint));
            }
        }

        public long ArgsEnqueued
        {
            get { return _argsEnqueued; }
        }

        public long ArgsDequeued { get; private set; }

        public IEnumerable<EndPointCounter> EndPointCounters
        {
            get { return _endPointCounters.Values; }
        }

        public sealed class EndPointCounter
        {
            private readonly IPEndPoint _endPoint;

            public EndPointCounter(IPEndPoint endPoint)
            {
                _endPoint = endPoint;
                OpenCount = 0;
                CloseCount = 0;
                RecycleCount = 0;
                SendFrameCount = 0;
                ReceiveFrameCount = 0;
                ErrorCount = 0;
            }

            public IPEndPoint EndPoint
            {
                get { return _endPoint; }
            }

            public long OpenCount { get; private set; }
            public void IncrementOpenCount()
            {
                OpenCount++;
            }

            public long CloseCount { get; private set; }
            public void IncrementCloseCount()
            {
                CloseCount++;
            }

            public long RecycleCount { get; private set; }
            public void IncrementRecycleCount()
            {
                RecycleCount++;
            }

            public long SendFrameCount { get; private set; }
            public void IncrementSendFrameCount()
            {
                SendFrameCount++;
            }

            public long ReceiveFrameCount { get; private set; }
            public void IncrementReceiveFrameCount()
            {
                ReceiveFrameCount++;
            }

            public long ErrorCount { get; private set; }
            public void IncrementErrorCount()
            {
                ErrorCount++;
            }
        }
    }
}
