using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Apache.Cassandra;
using Cassandra.Client.Async;
using Cassandra.Client.Thrift;

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
            RunTest(DescribeVersion);
            //RunTest(GetSlice);
        }

        private static async Task<long[]> DescribeVersion(CassandraClient client)
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

        private static async Task<long[]> GetSlice(CassandraClient client)
        {
            var elapsedMs = new long[RequestCount];
            var stopwatch = new Stopwatch();
            var args = new GetSliceArgs(EndPoint, "Keyspace#1")
            {
                Key = BitConverter.GetBytes(10),
                Column_parent = new ColumnParent(),
                Predicate = new SlicePredicate(),
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
            var client = new CassandraClient(ConcurrentClients).RunAsync();
            var clients = new Task<long[]>[ConcurrentClients];

            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < ConcurrentClients; i++)
            {
                clients[i] = test(client);
            }
            Task.WaitAll(clients);
            var totalElapsed = stopwatch.Elapsed;

            var elapsedMs = clients.SelectMany(c => c.Result).OrderBy(e => e).ToArray();

            Console.WriteLine("Throughput: {0:#.##} req/s", TotalRequests / totalElapsed.TotalSeconds);
            Console.WriteLine();
            Console.WriteLine("20%: {0}", elapsedMs[(int)(TotalRequests * .2)]);
            Console.WriteLine("50%: {0}", elapsedMs[(int)(TotalRequests * .5)]);
            Console.WriteLine("80%: {0}", elapsedMs[(int)(TotalRequests * .8)]);
            Console.WriteLine("95%: {0}", elapsedMs[(int)(TotalRequests * .95)]);
            Console.WriteLine("99%: {0}", elapsedMs[(int)(TotalRequests * .99)]);
            Console.WriteLine();
        }
    }
}
