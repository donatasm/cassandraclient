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
    }
}
