using System;

namespace Cassandra.Client
{
    public delegate void ResultCb(ITransport transport, Exception exception);
}