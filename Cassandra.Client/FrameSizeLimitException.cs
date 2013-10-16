using System;

namespace Cassandra.Client
{
    public sealed class FrameSizeLimitException : Exception
    {
        internal FrameSizeLimitException(int maxFrameSize)
            : base(String.Format("Maximum frame size of {0} byte(s) exceeded.",
                maxFrameSize))
        {
        }
    }
}