#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        CassandraContext::CassandraContext(InputProtocol^ input, OutputProtocol^ output, CassandraClient^ client)
        {
            _input = input;
            _output = output;
            _address = "127.0.0.1";
            _port = 1337;
            _client = client;
        }
    }
}