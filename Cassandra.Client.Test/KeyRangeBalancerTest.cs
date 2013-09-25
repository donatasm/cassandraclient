using System.Net;
using System.Numerics;
using Cassandra.Client.Async;
using NUnit.Framework;

namespace Cassandra.Client.Test
{
    [TestFixture]
    internal sealed class KeyRangeBalancerTest
    {
        [Test]
        public void GetEndPointBalancesBetweenEndPointsThatBelongToKeyRange()
        {
            var endPoint196 = CreateIPEndPoint("1.1.1.196");
            var endPoint197 = CreateIPEndPoint("1.1.1.197");
            var endPoint198 = CreateIPEndPoint("1.1.1.198");
            var tokenRanges = new[]
                {
                    new Ring.TokenRange(BigInteger.Parse("0"), BigInteger.Parse("56713727820156410577229101238628035242"),
                        new[]
                        {
                            endPoint197,
                            endPoint198
                        }),
                    new Ring.TokenRange(BigInteger.Parse("56713727820156410577229101238628035242"), BigInteger.Parse("113427455640312821154458202477256070484"),
                        new[]
                        {
                            endPoint198,
                            endPoint196
                        }),
                    new Ring.TokenRange(BigInteger.Parse("113427455640312821154458202477256070484"), BigInteger.Parse("0"),
                        new[]
                        {
                            endPoint196,
                            endPoint197
                        })
                };
            var key = new byte[] { 243, 224, 1, 0, 0, 0, 0, 0 };

            var balancer = new KeyRangeBalancer(tokenRanges);
            Assert.AreEqual(endPoint197, balancer.GetEndPoint(key));
            Assert.AreEqual(endPoint198, balancer.GetEndPoint(key));
            Assert.AreEqual(endPoint197, balancer.GetEndPoint(key));
            Assert.AreEqual(endPoint198, balancer.GetEndPoint(key));
            Assert.AreEqual(endPoint197, balancer.GetEndPoint(key));
            Assert.AreEqual(endPoint198, balancer.GetEndPoint(key));
            Assert.AreEqual(endPoint197, balancer.GetEndPoint(key));
            Assert.AreEqual(endPoint198, balancer.GetEndPoint(key));
            Assert.AreEqual(endPoint197, balancer.GetEndPoint(key));
        }

        private static IPEndPoint CreateIPEndPoint(string ipString)
        {
            const int port = 9160;
            var ipAddress = IPAddress.Parse(ipString);
            return new IPEndPoint(ipAddress, port);
        }
    }
}
