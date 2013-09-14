#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        CassandraContext::CassandraContext(IArgs^ args, ResultCallback^ resultCallback, CassandraClient^ client)
        {
            _args = args;
            _resultCallback = resultCallback;
            _address = "127.0.0.1";
            _port = 1337;
            _client = client;
        }
    }
}