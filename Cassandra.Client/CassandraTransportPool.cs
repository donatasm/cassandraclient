using System.Collections.Generic;
using System.Net;

namespace Cassandra.Client
{
    public sealed class CassandraTransportPool
    {
        private readonly IDictionary<IPEndPoint, Queue<ICassandraTransport>> _pool;

        public CassandraTransportPool()
        {
            _pool = new Dictionary<IPEndPoint, Queue<ICassandraTransport>>();
        }

        public bool TryGet(IPEndPoint endPoint, out ICassandraTransport transport)
        {
            Queue<ICassandraTransport> endPointPool;

            if (_pool.TryGetValue(endPoint, out endPointPool) && (endPointPool.Count > 0))
            {
                transport = endPointPool.Dequeue();
                return true;
            }

            transport = null;
            return false;
        }

        public void Add(ICassandraTransport transport)
        {
            Queue<ICassandraTransport> endPointPool;

            if (!_pool.TryGetValue(transport.EndPoint, out endPointPool))
            {
                endPointPool = new Queue<ICassandraTransport>();
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
