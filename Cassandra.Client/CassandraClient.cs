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
        private readonly IUvLoop _loop;
        private readonly ITransportFactory _transportFactory;
        private readonly CassandraClientStats _stats;

        // wait handle for signaling loop stop
        private readonly ManualResetEventSlim _loopStop;

        // uv handle for stopping the loop
        private readonly IUvAsync _asyncStop;

        // uv handle for notifying the loop about new requests
        private readonly IUvAsync _asyncSend;

        private readonly ConcurrentQueue<CassandraContext> _contextQueue;

        public CassandraClient()
            : this(new UvFramedTransport.Factory(),
                new CassandraClientStats())
        {
        }

        public CassandraClient(CassandraClientStats stats)
            : this(new UvFramedTransport.Factory(), stats)
        {
        }

        public CassandraClient(ITransportFactory transportFactory,
            CassandraClientStats stats)
            : this(new UvLoop(), transportFactory, stats)
        {
        }

        internal CassandraClient(IUvLoop loop,
            ITransportFactory transportFactory,
            CassandraClientStats stats)
        {
            _loop = loop;
            _transportFactory = transportFactory;
            _stats = stats;

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
