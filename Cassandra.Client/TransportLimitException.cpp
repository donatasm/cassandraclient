#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        TransportLimitException::TransportLimitException(String^ message) : Exception(message)
        {
        }
    }
}