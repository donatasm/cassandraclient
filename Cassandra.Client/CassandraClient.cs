using System;
using System.Diagnostics;
using System.Threading;
using NetUv;

namespace Cassandra.Client
{
    public sealed class CassandraClient : IDisposable
    {
        private readonly IUvLoop _loop;
        private readonly CassandraClientStats _stats;
        private readonly int _maxEndPointTransportCount;

        // wait handle for signaling loop stop
        private readonly ManualResetEventSlim _loopStop;

        // uv handle for stopping the loop
        private readonly IUvAsync _asyncStop;

        public CassandraClient(int maxEndPointTransportCount = 64)
            : this(new CassandraClientStats(), maxEndPointTransportCount)
        {
        }

        public CassandraClient(CassandraClientStats stats, int maxEndPointTransportCount)
            : this(new UvLoop(), stats, maxEndPointTransportCount)
        {
        }

        internal CassandraClient(IUvLoop loop, CassandraClientStats stats, int maxEndPointTransportCount)
        {
            _loop = loop;
            _stats = stats;
            _maxEndPointTransportCount = maxEndPointTransportCount;

            _loopStop = new ManualResetEventSlim();

            // close all active loop handles, so loop thread can return
            _asyncStop = _loop.InitUvAsync((async, exception) => CloseAllHandles());
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

        private void CloseAllHandles()
        {
            Debug.WriteLine("CloseAllHandles on thread ID={0}", Thread.CurrentThread.ManagedThreadId);

            _asyncStop.Close(h => h.Dispose());
        }
    }
}
