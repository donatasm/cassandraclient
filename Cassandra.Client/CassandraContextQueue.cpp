#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        CassandraContextQueue::CassandraContextQueue()
        {
            _handle = GCHandle::Alloc(this);
            _queue = gcnew ConcurrentQueue<CassandraContext^>();
        }


        CassandraContextQueue::~CassandraContextQueue()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }


        void CassandraContextQueue::Enqueue(CassandraContext^ context)
        {
            _queue->Enqueue(context);
        }


        bool CassandraContextQueue::TryDequeue(CassandraContext^ %context)
        {
            return _queue->TryDequeue(context);
        }


        void* CassandraContextQueue::ToPointer()
        {
            return GCHandle::ToIntPtr(_handle).ToPointer();
        }


        CassandraContextQueue^ CassandraContextQueue::FromPointer(void* ptr)
        {
            GCHandle handle = GCHandle::FromIntPtr(IntPtr(ptr));
            return (CassandraContextQueue^)handle.Target;
        }
    }
}