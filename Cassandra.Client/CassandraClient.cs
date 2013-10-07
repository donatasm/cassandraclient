using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Cassandra.Client.Thrift;
using NetUv;

namespace Cassandra.Client
{
    public sealed class CassandraClient : IDisposable
    {
        private const int DefaultMaxTransports = 64;

        private readonly IUvLoop _loop;
        private readonly CassandraClientStats _stats;
        private readonly int _maxEndPointTransportCount;

        // wait handle for signaling loop stop
        private readonly ManualResetEventSlim _loopStop;

        // uv handle for stopping the loop
        private readonly IUvAsync _asyncStop;

        // uv handle for notifying the loop about new requests
        private readonly IUvAsync _asyncSend;

        private readonly ConcurrentQueue<CassandraContext> _contextQueue;

        public CassandraClient(int maxEndPointTransportCount = DefaultMaxTransports)
            : this(new CassandraClientStats(), maxEndPointTransportCount)
        {
        }

        public CassandraClient(CassandraClientStats stats, int maxEndPointTransportCount = DefaultMaxTransports)
            : this(new UvLoop(), stats, maxEndPointTransportCount)
        {
        }

        internal CassandraClient(IUvLoop loop, CassandraClientStats stats, int maxEndPointTransportCount)
        {
            _loop = loop;
            _stats = stats;
            _maxEndPointTransportCount = maxEndPointTransportCount;

            _loopStop = new ManualResetEventSlim();
            _contextQueue = new ConcurrentQueue<CassandraContext>();

            // close all active loop handles, so loop thread can return
            _asyncStop = _loop.InitUvAsync((async, exception) => CloseAllHandles());

            // notify context queue
            _asyncSend = _loop.InitUvAsync((async, exception) => ProcessContextQueue());
        }

        public void SendAsync(IArgs args, Action<Exception> resultCb)
        {
            var context = new CassandraContext();

            // enqueue and notify the loop about contexts pending
            _contextQueue.Enqueue(context);
            _asyncSend.Send();
            _stats.IncrementArgsEnqueued();
        }

        public void RunAsync()
        {
            Debug.WriteLine("Main thread ID={0}", Thread.CurrentThread.ManagedThreadId);

            new Thread(() =>
                {
                    Debug.WriteLine("Loop thread ID={0}", Thread.CurrentThread.ManagedThreadId);
                    _loop.Run();
                    _loopStop.Set();
                })
                {
                    IsBackground = true
                }
            .Start();
        }

        public void Dispose()
        {
            Debug.WriteLine("Disposing on thread ID={0}", Thread.CurrentThread.ManagedThreadId);

            // notify loop to stop
            _asyncStop.Send();

            // wait until loop stops
            _loopStop.Wait();

            // check if loop can be disposed
            var loop = _loop as IDisposable;

            if (loop != null)
            {
                loop.Dispose();
            }

            // dispose wait handle
            _loopStop.Dispose();
        }

        private void ProcessContextQueue()
        {
            Debug.WriteLine("ProcessContextQueue on thread ID={0}", Thread.CurrentThread.ManagedThreadId);

            CassandraContext context;

            while (_contextQueue.TryDequeue(out context))
            {
                _stats.IncrementArgsDequeued();
            }
        }

        private void CloseAllHandles()
        {
            Debug.WriteLine("CloseAllHandles on thread ID={0}", Thread.CurrentThread.ManagedThreadId);

            _asyncSend.Close(h => h.Dispose());
            _asyncStop.Close(h => h.Dispose());
        }
    }

    internal sealed class CassandraContext
    {
    }
}
