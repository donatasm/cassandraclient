using System.Threading;
using Cassandra.Client.Thrift;
using Moq;
using NUnit.Framework;

namespace Cassandra.Client.Test
{
    [TestFixture]
    internal sealed class CassandraClientTest
    {
        [Test]
        public void Dispose()
        {
            using (var client = new CassandraClient())
            {
                client.RunAsync();
            }
        }

        [Test]
        public void SendAsync()
        {
            const int argsCount = 1000000;
            var argsEnqueued = 0;
            var argsDequeued = 0;

            using (var signal = new ManualResetEventSlim())
            {
                var stats = new Mock<CassandraClientStats>();
                stats
                    .Setup(s => s.IncrementArgsEnqueued())
                    .Callback(() =>
                    {
                        argsEnqueued++;
                    });
                stats
                    .Setup(s => s.IncrementArgsDequeued())
                    .Callback(() =>
                        {
                            argsDequeued++;

                            if (argsDequeued == argsCount)
                            {
                                // ReSharper disable AccessToDisposedClosure
                                signal.Set();
                                // ReSharper restore AccessToDisposedClosure
                            }
                        });

                using (var client = new CassandraClient(stats.Object))
                {
                    client.RunAsync();

                    for (var i = 0; i < argsCount; i++)
                    {
                        var args = new Mock<IArgs>();
                        client.SendAsync(args.Object, (exception) => {});
                    }

                    signal.Wait();
                }
            }

            Assert.AreEqual(argsCount, argsEnqueued);
            Assert.AreEqual(argsCount, argsDequeued);
        }
    }
}
