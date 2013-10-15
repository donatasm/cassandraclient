using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Apache.Cassandra;
using Cassandra.Client.Async;
using Cassandra.Client.Thrift;
using Thrift.Transport;

namespace Cassandra.Client.Test
{
    internal static class Run
    {
        private const int ConcurrentClients = 256;
        private const int RequestCount = 1000;
        private const int TotalRequests = ConcurrentClients * RequestCount;

        private static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1337);

        private static void Main()
        {
            RunTest(DescribeVersionDoNotWaitForResult);
            RunTest(DescribeVersionWaitForResult);
            RunTest(GetSliceWaitForResult);
        }

        private static Task<long[]> DescribeVersionDoNotWaitForResult(CassandraClient client)
        {
            var elapsedMs = new long[RequestCount];
            var tasks = new Task[RequestCount];

            for (var i = 0; i < RequestCount; i++)
            {
                var args = new DescribeVersionArgs(EndPoint);
                var stopwatch = Stopwatch.StartNew();
                var j = i;
                tasks[i] = client.DescribeVersionAsync(args)
                    .ContinueWith(t =>
                    {
                        elapsedMs[j] = stopwatch.ElapsedMilliseconds;

                        if (t.IsFaulted)
                        {
                            // ReSharper disable PossibleNullReferenceException
                            t.Exception.Handle(e =>
                                e is TTransportException
                                || e is TransportLimitException);
                            // ReSharper restore PossibleNullReferenceException
                        }
                    });
            }

            Task.WaitAll(tasks);

            return Task.FromResult(elapsedMs);
        }

        private static async Task<long[]> DescribeVersionWaitForResult(CassandraClient client)
        {
            var elapsedMs = new long[RequestCount];
            var stopwatch = new Stopwatch();
            var args = new DescribeVersionArgs(EndPoint);

            for (var i = 0; i < RequestCount; i++)
            {
                stopwatch.Restart();
                await client.DescribeVersionAsync(args);
                elapsedMs[i] = stopwatch.ElapsedMilliseconds;
            }

            return elapsedMs;
        }

        private static async Task<long[]> GetSliceWaitForResult(CassandraClient client)
        {
            var elapsedMs = new long[RequestCount];
            var stopwatch = new Stopwatch();
            var args = new GetSliceArgs(EndPoint, "Keyspace#1")
            {
                Key = BitConverter.GetBytes(10),
                Column_parent = new ColumnParent
                    {
                        Column_family = "RealTimeBiddingBidData"
                    },
                Predicate = new SlicePredicate
                    {
                        Slice_range = new SliceRange
                        {
                            Count = Int32.MaxValue,
                            Start = new byte[0],
                            Finish = new byte[0],
                            Reversed = false
                        }
                    },
                Consistency_level = ConsistencyLevel.ONE
            };

            for (var i = 0; i < RequestCount; i++)
            {
                stopwatch.Restart();
                await client.GetSliceAsync(args);
                elapsedMs[i] = stopwatch.ElapsedMilliseconds;
            }

            return elapsedMs;
        }

        private static void RunTest(Func<CassandraClient, Task<long[]>> test)
        {
            var stats = new ConsoleCassandraClientStats();
            var clients = new Task<long[]>[ConcurrentClients];
            TimeSpan totalElapsed;

            var factory = new UvFramedTransport.Factory(stats, ConcurrentClients);
            using (var client = new CassandraClient(factory, stats))
            {
                client.RunAsync();

                var stopwatch = Stopwatch.StartNew();

                for (var i = 0; i < ConcurrentClients; i++)
                {
                    clients[i] = test(client);
                }

                Task.WaitAll(clients);

                totalElapsed = stopwatch.Elapsed;
            }

            var elapsedMs = clients.SelectMany(c => c.Result).OrderBy(e => e).ToArray();

            Console.WriteLine("Throughput: {0:#.##} req/s", TotalRequests / totalElapsed.TotalSeconds);
            Console.WriteLine();
            Console.WriteLine("20%: {0}", elapsedMs[(int)(TotalRequests * .2)]);
            Console.WriteLine("50%: {0}", elapsedMs[(int)(TotalRequests * .5)]);
            Console.WriteLine("80%: {0}", elapsedMs[(int)(TotalRequests * .8)]);
            Console.WriteLine("95%: {0}", elapsedMs[(int)(TotalRequests * .95)]);
            Console.WriteLine("99%: {0}", elapsedMs[(int)(TotalRequests * .99)]);
            Console.WriteLine();
            Console.WriteLine("Args enqueued {0}", stats.ArgsEnqueued);
            Console.WriteLine("Args dequeued {0}", stats.ArgsDequeued);
            Console.WriteLine();
            foreach (var counter in stats.EndPointCounters)
            {
                Console.WriteLine("{0}:", counter.EndPoint);
                Console.WriteLine("\topen count: {0}", counter.OpenCount);
                Console.WriteLine("\tclose count: {0}", counter.CloseCount);
                Console.WriteLine("\tsend frame count: {0}", counter.SendFrameCount);
                Console.WriteLine("\treceive frame count: {0}", counter.ReceiveFrameCount);
            }
            Console.WriteLine();
        }
    }
}
