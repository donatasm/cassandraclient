#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        CassandraTransportPool::CassandraTransportPool()
        {
            _pool = gcnew Dictionary<IPEndPoint^, Queue<CassandraTransport^>^>();
        }


        void CassandraTransportPool::Add(CassandraTransport^ transport)
        {
            Queue<CassandraTransport^>^ endPointPool;

            if (!_pool->TryGetValue(transport->_endPoint, endPointPool))
            {
                endPointPool = gcnew Queue<CassandraTransport^>();
            }

            endPointPool->Enqueue(transport);
        }


        bool CassandraTransportPool::TryGet(IPEndPoint^ endPoint, CassandraTransport^ %transport)
        {
            Queue<CassandraTransport^>^ endPointPool;

            if (_pool->TryGetValue(endPoint, endPointPool) && (endPointPool->Count > 0))
            {
                transport = endPointPool->Dequeue();
                return true;
            }

            transport = nullptr;
            return false;
        }


        IEnumerable<IPEndPoint^>^ CassandraTransportPool::GetEndPoints()
        {
            return _pool->Keys;
        }
    }
}