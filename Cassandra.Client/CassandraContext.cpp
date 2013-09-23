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


        void CassandraContext::SetError(Exception^ exception)
        {
            _client->_stats->IncrementTransportError(_args->EndPoint);
            _resultCallback(nullptr, exception);
        }


        void CassandraContext::SetError(int error)
        {
            SetError(UvException::CreateFrom(error));
        }
    }
}