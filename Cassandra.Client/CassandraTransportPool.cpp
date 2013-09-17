#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        CassandraTransportPool::CassandraTransportPool()
        {
            _pool = gcnew Dictionary<IPEndPoint^, Queue<ICassandraTransport^>^>();
        }


        void CassandraTransportPool::Add(ICassandraTransport^ transport)
        {
            Queue<ICassandraTransport^>^ endPointPool;

            if (!_pool->TryGetValue(transport->EndPoint, endPointPool))
            {
                endPointPool = gcnew Queue<ICassandraTransport^>();
                _pool->Add(transport->EndPoint, endPointPool);
            }

            endPointPool->Enqueue(transport);
        }


        ICassandraTransport^ CassandraTransportPool::Get(IPEndPoint^ endPoint)
        {
            Queue<ICassandraTransport^>^ endPointPool;

            if (_pool->TryGetValue(endPoint, endPointPool) && (endPointPool->Count > 0))
            {
                return endPointPool->Dequeue();
            }

            return nullptr;
        }


        IEnumerable<IPEndPoint^>^ CassandraTransportPool::GetEndPoints()
        {
            return _pool->Keys;
        }
    }
}