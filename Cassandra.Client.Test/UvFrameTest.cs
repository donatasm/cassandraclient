using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using NetUv;

namespace Cassandra.Client.Test
{
    [TestFixture]
    internal sealed class UvFrameTest
    {
        [Test]
        public void FlushEmptyFrame()
        {
            var frame = new UvFrame();
            var buffer = frame.Flush();

            Assert.AreEqual(0, buffer.Offset);
            Assert.AreEqual(4, buffer.Count);
            CollectionAssert.AreEqual(new byte[]
                {
                //  | header |
                    0, 0, 0, 0
                },
                buffer
                    .Array
                    .Skip(buffer.Offset)
                    .Take(buffer.Count));
        }

        [Test]
        public void FlushNotEmptyFrame()
        {
            var frame = new UvFrame();
            frame.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
            frame.Write(new byte[] { 0, 0, 6, 7, 8 }, 2, 2);
            var buffer = frame.Flush();

            Assert.AreEqual(0, buffer.Offset);
            Assert.AreEqual(11, buffer.Count);
            CollectionAssert.AreEqual(new byte[]
                {
                //  | header |  |       body      |
                    0, 0, 0, 7, 1, 2, 3, 4, 5, 6, 7
                },
                buffer
                    .Array
                    .Skip(buffer.Offset)
                    .Take(buffer.Count));
        }

        [Test]
        public void ReadThrowsInvalidOperationExceptionIfFrameReadIsNotCompleted()
        {
            var frame = new UvFrame();
            var buffer = new byte[1];

            Assert.Throws<InvalidOperationException>(() => frame.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void AllocBufferRead()
        {
            const int maxFrameSize = 603;
            const int header = 4;

            var frame = new UvFrame(maxFrameSize);

            // allocate
            var b1 = frame.AllocBuffer();
            Assert.AreEqual(0, b1.Offset);
            Assert.AreEqual(maxFrameSize + header, b1.Count);

            // read
            frame.Read(b1, 500);

            // allocate
            var b2 = frame.AllocBuffer();
            Assert.AreEqual(500, b2.Offset);
            Assert.AreEqual(103 + header, b2.Count);

            // read
            frame.Read(b2, 1);

            // allocate
            var b3 = frame.AllocBuffer();
            Assert.AreEqual(501, b3.Offset);
            Assert.AreEqual(102 + header, b3.Count);
        }

        [Test]
        public void AllocBufferRead0()
        {
            var frame = new UvFrame();
            var buffer = frame.AllocBuffer();
            frame.Read(buffer, 0);
        }

        [Test]
        public void IsBodyReadNewFrame()
        {
            var frame = new UvFrame();
            Assert.IsFalse(frame.IsBodyRead);
        }

        [Test]
        public void IsBodyReadReturnsTrueFrameHeaderIsReceived()
        {
            var frame = new UvFrame();
            var uvBuffer = frame.AllocBuffer();
            uvBuffer.Array[0] = 0;
            uvBuffer.Array[1] = 0;
            uvBuffer.Array[2] = 0;
            uvBuffer.Array[3] = 0;
            frame.Read(uvBuffer, 4);

            Assert.IsTrue(frame.IsBodyRead);
            Assert.AreEqual(0, frame.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()));
        }

        [Test]
        public void WriteThrowsFrameSizeLimitException()
        {
            const int frameSize = 4;
            var frameBody = new byte[frameSize + 1];

            var frame = new UvFrame(frameSize);
            Assert.DoesNotThrow(() => frame.Write(frameBody, 0, frameSize));
            Assert.Throws<FrameSizeLimitException>(() => frame.Write(frameBody, frameSize, 1));
        }

        [Test]
        public void AllocThrowsFrameSizeLimitException()
        {
            const int frameSize = 4;

            var frame = new UvFrame(frameSize);
            var buffer = frame.AllocBuffer();
            Assert.AreEqual(8, buffer.Array.Length);
            Assert.AreEqual(0, buffer.Offset);
            Assert.AreEqual(8, buffer.Count);
            Assert.DoesNotThrow(() => frame.Read(buffer, 8));
            Assert.Throws<FrameSizeLimitException>(() => frame.AllocBuffer());
        }
    }
}
