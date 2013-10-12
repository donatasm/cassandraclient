using System.Collections.Generic;
using System.Net;

namespace Cassandra.Client
{
    public sealed class CassandraTransportPool : ITransportPool
    {
        private readonly IDictionary<IPEndPoint, Queue<ITransport>> _pool;

        public CassandraTransportPool()
        {
            _pool = new Dictionary<IPEndPoint, Queue<ITransport>>();
        }

        public bool TryGet(IPEndPoint endPoint, out ITransport transport)
        {
            Queue<ITransport> endPointPool;

            if (_pool.TryGetValue(endPoint, out endPointPool) && (endPointPool.Count > 0))
            {
                transport = endPointPool.Dequeue();
                return true;
            }

            transport = null;
            return false;
        }

        public void Add(ITransport transport)
        {
            Queue<ITransport> endPointPool;

            if (!_pool.TryGetValue(transport.EndPoint, out endPointPool))
            {
                endPointPool = new Queue<ITransport>();
                _pool.Add(transport.EndPoint, endPointPool);
            }

            endPointPool.Enqueue(transport);
        }

        public IEnumerable<IPEndPoint> GetEndPoints()
        {
            return _pool.Keys;
        }
    }
}
