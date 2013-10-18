using System.Linq;
using NUnit.Framework;

namespace Cassandra.Client.Test
{
    [TestFixture]
    internal sealed class UvFrameTest
    {
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
            Assert.AreEqual(0, frame.Read(new byte[0], 0, 0));
        }
    }
}
