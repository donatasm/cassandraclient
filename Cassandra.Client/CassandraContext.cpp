#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        CassandraContext::CassandraContext(IArgs^ args, Result^ result, CassandraClient^ client)
        {
            _args = args;
            _result = result;
            _address = "127.0.0.1";
            _port = 1337;
            _client = client;
        }
    }
}