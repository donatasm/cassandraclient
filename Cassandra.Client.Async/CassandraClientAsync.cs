﻿using System.Collections.Generic;
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

        public static Task<List<ColumnOrSuperColumn>> GetSliceAsync(this CassandraClient client, GetSliceArgs args)
        {
            return client.SendAsync(args, new GetSliceResult());
        }

        private static Task<TResult> SendAsync<TResult>(this CassandraClient client, IArgs args, IResult<TResult> result)
        {
            var tcs = new TaskCompletionSource<TResult>();

            client.Send(args, (protocol, exception) =>
                {
                    var transport = (ICassandraTransport)protocol.Transport;

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

                        transport.Recycle();
                    }
                    else
                    {
                        tcs.TrySetException(exception);

                        transport.Close();
                    }
                });

            return tcs.Task;
        }
    }
}