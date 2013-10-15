using System;
using System.Net;

namespace Cassandra.Client
{
    public sealed class TransportLimitException : Exception
    {
        private readonly EndPoint _endPoint;

        public TransportLimitException(string message, EndPoint endPoint)
            : base(message)
        {
            _endPoint = endPoint;
        }

        public EndPoint EndPoint
        {
            get { return _endPoint; }
        }
    }
}