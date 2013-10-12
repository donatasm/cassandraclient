using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Cassandra;
using Cassandra.Client.Thrift;

namespace Cassandra.Client.Async
{
    public static class CassandraClientAsync
    {
        public static Task<string> DescribeVersionAsync(this CassandraClient client, DescribeVersionArgs args)
        {
            return client.SendAsync(args, new DescribeVersionResult());
        }

        public static Task<List<TokenRange>> DescribeRingAsync(this CassandraClient client, DescribeRingArgs args)
        {
            return client.SendAsync(args, new DescribeRingResult());
        }

        public static Task<List<ColumnOrSuperColumn>> GetSliceAsync(this CassandraClient client, GetSliceArgs args)
        {
            return client.SendAsync(args, new GetSliceResult());
        }

        private static Task<TResult> SendAsync<TResult>(this CassandraClient client, IArgs args, IResult<TResult> result)
        {
            var tcs = new TaskCompletionSource<TResult>();

            client.SendAsync(args, (transport, exception) =>
                {
                    // check for transport exceptions
                    if (exception == null)
                    {
                        result.ReadMessage(transport.Protocol);

                        // check for protocol exceptions too
                        if (result.Exception == null)
                        {
                            tcs.TrySetResult(result.Success);
                        }
                        else
                        {
                            tcs.TrySetException(result.Exception);
                        }
                    }
                    else
                    {
                        tcs.TrySetException(exception);
                    }
                });

            return tcs.Task;
        }
    }
}
