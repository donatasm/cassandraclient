using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Cassandra.Client.Thrift;
using NetUv;
using log4net;

namespace Cassandra.Client
{
    public sealed class CassandraClient : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CassandraClient));

        private readonly IUvLoop _loop;
        private readonly ITransportFactory _transportFactory;
        private readonly IClientStats _clientStats;

        // wait handle for signaling loop stop
        private readonly ManualResetEventSlim _loopStop;

        // uv handle for stopping the loop
        private readonly IUvAsync _asyncStop;

        // uv handle for notifying the loop about new requests
        private readonly IUvAsync _asyncSend;

        private readonly ConcurrentQueue<CassandraContext> _contextQueue;

        private readonly ITransportPool _transportPool;

        public CassandraClient()
            : this(new UvFramedTransport.Factory(DefaultClientStats),
                DefaultClientStats)
        {
        }

        public CassandraClient(IClientStats clientStats)
            : this(new UvFramedTransport.Factory(clientStats), clientStats)
        {
        }

        public CassandraClient(ITransportFactory transportFactory,
            IClientStats clientstats)
            : this(UvLoop.Default, transportFactory, clientstats)
        {
        }

        internal CassandraClient(IUvLoop loop,
            ITransportFactory transportFactory,
            IClientStats clientStats)
        {
            _loop = loop;
            _transportFactory = transportFactory;
            _clientStats = clientStats;

            _loopStop = new ManualResetEventSlim();
            _contextQueue = new ConcurrentQueue<CassandraContext>();
            _transportPool = new CassandraTransportPool();

            // close all active loop handles, so loop thread can return
            _asyncStop = _loop.InitUvAsync((async, exception) => CloseAllHandles());

            // notify context queue
            _asyncSend = _loop.InitUvAsync((async, exception) => ProcessContextQueue());
        }

        public void SendAsync(IArgs args, ResultCb resultCb)
        {
            var context = new CassandraContext(args, resultCb);

            // enqueue and notify the loop about contexts pending
            _contextQueue.Enqueue(context);
            _asyncSend.Send();
            _clientStats.IncrementArgsEnqueued();
        }

        public void RunAsync()
        {
            Debug.WriteLine("Main thread ID={0}", Thread.CurrentThread.ManagedThreadId);

            new Thread(() =>
                {
                    Debug.WriteLine("Loop thread ID={0}", Thread.CurrentThread.ManagedThreadId);

                    try
                    {
                        _loop.Run();
                    }
                    catch (Exception e)
                    {
                        Log.Error("Loop thread error.", e);
                    }

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

            // it is not nice to dispose the default loop
            if (!ReferenceEquals(_loop, UvLoop.Default))
            {
                // check if loop implements disposable
                var loop = _loop as IDisposable;

                if (loop != null)
                {
                    loop.Dispose();
                }
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
                try
                {
                    ProcessContext(context);
                }
                finally
                {
                    _clientStats.IncrementArgsDequeued();
                }
            }
        }

        private void ProcessContext(CassandraContext context)
        {
            var endPoint = context.Args.EndPoint;

            ITransport transport;
            if (!_transportPool.TryGet(endPoint, out transport))
            {
                if (!_transportFactory.TryCreate(endPoint, out transport))
                {
                    context.ResultCb(null,
                        new TransportLimitException("Transport limit reached.",
                            endPoint));
                    return;
                }
            }

            var wrappedContext = GetKeyspaceWrappedContext(transport, context);
            wrappedContext.SendArgs(transport);
        }

        private CassandraContext GetKeyspaceWrappedContext(ITransport transport, CassandraContext context)
        {
            if (TransportKeyspaceIsNotSet(transport, context.Args))
            {
                var keyspace = context.Args.Keyspace;
                var setKeyspaceArgs = new SetKeyspaceArgs(transport.EndPoint, keyspace);

                return new CassandraContext(setKeyspaceArgs,
                    (trans, exception) =>
                        {
                            if (exception != null)
                            {
                                context.ResultCb(trans, exception);
                                return;
                            }

                            transport.Keyspace = keyspace;
                            context.TransportPool = _transportPool; // return to pool only after keyspace is set
                            context.SendArgs(trans);
                        });
            }

            context.TransportPool = _transportPool;
            return context;
        }

        private static bool TransportKeyspaceIsNotSet(ITransport transport, IArgs args)
        {
            return args.Keyspace != null && transport.Keyspace != args.Keyspace;
        }

        private void CloseAllHandles()
        {
            Debug.WriteLine("CloseAllHandles on thread ID={0}", Thread.CurrentThread.ManagedThreadId);

            _asyncSend.Close(h => h.Dispose());
            _asyncStop.Close(h => h.Dispose());
        }

        private static readonly IClientStats DefaultClientStats = new ClientStats();

        private class ClientStats : IClientStats
        {
            public void IncrementTransportOpen(EndPoint endPoint)
            {
            }

            public void IncrementTransportClose(EndPoint endPoint)
            {
            }

            public void IncrementTransportSendFrame(EndPoint endPoint)
            {
            }

            public void IncrementTransportReceiveFrame(EndPoint endPoint)
            {
            }

            public void IncrementArgsEnqueued()
            {
            }

            public void IncrementArgsDequeued()
            {
            }
        }
    }
}
