using System.Threading.Tasks;
using Cassandra.Client.Thrift;

namespace Cassandra.Client.Async
{
    public static class CassandraClientAsync
    {
        public static Task<string> DescribeVersionAsync(this CassandraClient client)
        {
            return client.SendAsync(new DescribeVersionArgs(), new DescribeVersionResult());
        }

        private static Task<TResult> SendAsync<TResult>(this CassandraClient client, IArgs args, IResult<TResult> result)
        {
            var tcs = new TaskCompletionSource<TResult>();

            client.Send(args, (protocol, exception) =>
                {
                    // check for transport exceptions
                    if (exception == null)
                    {
                        result.ReadMessage(protocol);

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
