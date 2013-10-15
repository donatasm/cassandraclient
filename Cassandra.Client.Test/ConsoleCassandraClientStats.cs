using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Cassandra.Client.Test
{
    internal sealed class ConsoleCassandraClientStats : IClientStats
    {
        private long _argsEnqueued;
        private readonly Dictionary<IPEndPoint, EndPointCounter> _endPointCounters;

        public ConsoleCassandraClientStats()
        {
            _argsEnqueued = 0;
            _endPointCounters = new Dictionary<IPEndPoint, EndPointCounter>();
            ArgsDequeued = 0;
        }

        public void IncrementArgsEnqueued()
        {
            Interlocked.Increment(ref _argsEnqueued);
        }

        public void IncrementArgsDequeued()
        {
            ArgsDequeued++;
        }

        public void IncrementTransportOpen(IPEndPoint endPoint)
        {
            AddIfNotExists(endPoint);
            _endPointCounters[endPoint].IncrementOpenCount();
        }

        public void IncrementTransportClose(IPEndPoint endPoint)
        {
            AddIfNotExists(endPoint);
            _endPointCounters[endPoint].IncrementCloseCount();
        }

        public void IncrementTransportSendFrame(IPEndPoint endPoint)
        {
            AddIfNotExists(endPoint);
            _endPointCounters[endPoint].IncrementSendFrameCount();
        }

        public void IncrementTransportReceiveFrame(IPEndPoint endPoint)
        {
            AddIfNotExists(endPoint);
            _endPointCounters[endPoint].IncrementReceiveFrameCount();
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
                SendFrameCount = 0;
                ReceiveFrameCount = 0;
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
        }
    }
}
