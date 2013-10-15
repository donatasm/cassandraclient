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
        private static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, 9160);

        [Test]
        public void CreatedTransportIsOpenReturnsFalse()
        {
            var factory = new UvFramedTransport.Factory(It.IsAny<IFramedTransportStats>());

            ITransport transport = null;

            try
            {
                factory.TryCreate(EndPoint, out transport);
                Assert.IsNotNull(transport);
                Assert.IsFalse(transport.IsOpen);
            }
            finally
            {
                if (transport != null)
                {
                    transport.Dispose();
                }
            }
        }

        [Test]
        public void CreatedTransportReturnsLoopbackAddressForEndPointNotSet()
        {
            var factory = new UvFramedTransport.Factory(It.IsAny<IFramedTransportStats>());

            ITransport transport = null;

            try
            {
                factory.TryCreate(EndPoint, out transport);

                Assert.IsNotNull(transport);
                Assert.AreEqual(IPAddress.Loopback, transport.EndPoint.Address);
            }
            finally
            {
                if (transport != null)
                {
                    transport.Dispose();
                }
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

            var stats = new Mock<IFramedTransportStats>();

            var openCbCalled = false;

            var factory = new UvFramedTransport.Factory(stats.Object);
            factory.SetUvTcpFactory(() => uvTcp.Object);

            ITransport transport = null;

            try
            {
                factory.TryCreate(EndPoint, out transport);

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
            finally
            {
                if (transport != null)
                {
                    transport.Dispose();
                }
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

            var stats = new Mock<IFramedTransportStats>();

            var openCbCalled = false;
            var closeCbCalled = false;

            var factory = new UvFramedTransport.Factory(stats.Object);
            factory.SetUvTcpFactory(() => uvTcp.Object);

            ITransport transport = null;

            try
            {
                factory.TryCreate(EndPoint, out transport);

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
            finally
            {
                if (transport != null)
                {
                    transport.Dispose();
                }
            }
        }

        [Test]
        public void FlushThrowsTTransportExceptionTransportIsNotOpened()
        {
            var factory = new UvFramedTransport.Factory(It.IsAny<IFramedTransportStats>());

            ITransport transport;

            factory.TryCreate(EndPoint, out transport);

            try
            {
                Assert.Throws<TTransportException>(transport.Flush);
            }
            finally
            {
                if (transport != null)
                {
                    transport.Dispose();
                }
            }
        }
    }
}
