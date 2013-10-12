using System;

namespace Cassandra.Client
{
    public sealed class TransportLimitException : Exception
    {
        public static readonly TransportLimitException Default = new TransportLimitException("Transport limit reached.");

        public TransportLimitException(string message)
            : base(message)
        {
        }
    }
}