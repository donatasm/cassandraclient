using System.Net;
using Moq;
using NUnit.Framework;

namespace Cassandra.Client.Test
{
    [TestFixture]
    internal sealed class CassandraTransportPoolTest
    {
        [Test]
        public void TryGetReturnsFalseForEmptyPool()
        {
            ITransport transport;
            var pool = new CassandraTransportPool();
            Assert.False(pool.TryGet(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1337), out transport));
        }

        [Test]
        public void AddedTransportIsReturnedByAPool()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1337);
            var transport = new Mock<ITransport>();
            transport.SetupGet(t => t.EndPoint).Returns(endPoint);

            var pool = new CassandraTransportPool();
            pool.Add(transport.Object);

            ITransport actualTransport;
            Assert.IsTrue(pool.TryGet(endPoint, out actualTransport));
            Assert.IsNotNull(actualTransport);
            Assert.AreEqual(transport.Object, actualTransport);
        }

        [Test]
        public void GetEndpointsReturnsAllEndPointsInAPool()
        {
            var endPoint1 = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1337);
            var endPoint2 = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1338);
            var endPoint3 = new IPEndPoint(IPAddress.Parse("127.0.0.2"), 1337);
            var transport1 = new Mock<ITransport>();
            transport1.SetupGet(t => t.EndPoint).Returns(endPoint1);
            var transport2 = new Mock<ITransport>();
            transport2.SetupGet(t => t.EndPoint).Returns(endPoint2);
            var transport3 = new Mock<ITransport>();
            transport3.SetupGet(t => t.EndPoint).Returns(endPoint3);

            var pool = new CassandraTransportPool();
            pool.Add(transport1.Object);
            pool.Add(transport2.Object);
            pool.Add(transport3.Object);

            CollectionAssert.AreEqual(new[] { endPoint1, endPoint2, endPoint3 }, pool.GetEndPoints());
        }
    }
}
