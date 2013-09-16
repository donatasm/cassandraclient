#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        CassandraContext::CassandraContext(IArgs^ args, ResultCallback^ resultCallback, CassandraClient^ client)
        {
            _args = args;
            _resultCallback = resultCallback;
            _client = client;
        }
    }
}