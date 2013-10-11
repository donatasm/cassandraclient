using System;
using System.Net;
using Moq;
using NUnit.Framework;
using NetUv;
using Thrift.Transport;

namespace Cassandra.Client.Test
{
    [TestFixture]
    internal sealed class UvFramedTransportTest
    {
        [Test]
        public void CreatedTransportIsOpenReturnsFalse()
        {
            var factory = new UvFramedTransport.Factory();

            using (var transport = factory.Create())
            {
                Assert.IsFalse(transport.IsOpen);
            }
        }

        [Test]
        public void CreatedTransportReturnsLoopbackAddressForEndPointNotSet()
        {
            var factory = new UvFramedTransport.Factory();

            using (var transport = factory.Create())
            {
                Assert.AreEqual(IPAddress.Loopback, transport.EndPoint.Address);
            }
        }

        [Test]
        public void IsOpenReturnsTrueAfterOpenCallback()
        {
            var uvTcp = new Mock<IUvTcp>();
            uvTcp
                .Setup(u => u.Connect(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<UvTcpCb>()))
                .Callback((string ip, int port, UvTcpCb cb) => cb(uvTcp.Object, null));

            var stats = new Mock<FramedTransportStats>();

            var openCbCalled = false;

            var factory = new UvFramedTransport.Factory();
            factory.SetUvTcpFactory(() => uvTcp.Object);
            factory.SetStats(stats.Object);

            using (var transport = factory.Create())
            {
                transport.OpenCb = (t, e) =>
                    {
                        openCbCalled = true;
                    };

                transport.Open();

                Assert.IsTrue(openCbCalled);
                Assert.IsTrue(transport.IsOpen);
                Assert.Throws<TTransportException>(transport.Open);
                stats.Verify(s => s.IncrementTransportOpen(It.IsAny<IPEndPoint>()), Times.Once);
            }
        }

        [Test]
        public void IsOpenReturnsFalseAfterCloseCallback()
        {
            var uvTcp = new Mock<IUvTcp>();
            var uvTcpDisposable = uvTcp.As<IDisposable>();
            uvTcp
                .Setup(u => u.Connect(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<UvTcpCb>()))
                .Callback((string ip, int port, UvTcpCb cb) => cb(uvTcp.Object, null));
            uvTcp
                .Setup(u => u.Close(
                    It.IsAny<UvCloseCb>()))
                .Callback((UvCloseCb cb) => cb(uvTcpDisposable.Object));

            var stats = new Mock<FramedTransportStats>();

            var openCbCalled = false;
            var closeCbCalled = false;

            var factory = new UvFramedTransport.Factory();
            factory.SetUvTcpFactory(() => uvTcp.Object);
            factory.SetStats(stats.Object);

            using (var transport = factory.Create())
            {
                transport.OpenCb = (t, e) =>
                    {
                        openCbCalled = true;
                    };

                transport.CloseCb = (t, e) =>
                    {
                        closeCbCalled = true;
                    };

                transport.Open();
                transport.Close();

                Assert.IsTrue(openCbCalled);
                Assert.IsTrue(closeCbCalled);
                Assert.IsFalse(transport.IsOpen);
                stats.Verify(s => s.IncrementTransportOpen(It.IsAny<IPEndPoint>()), Times.Once);
                stats.Verify(s => s.IncrementTransportClose(It.IsAny<IPEndPoint>()), Times.Once);
            }
        }

        [Test]
        public void FlushThrowsTTransportExceptionTransportIsNotOpened()
        {
            var factory = new UvFramedTransport.Factory();

            using (var transport = factory.Create())
            {
                Assert.Throws<TTransportException>(transport.Flush);
            }
        }
    }
}
