#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        SetKeyspaceContext::SetKeyspaceContext(CassandraContext^ context, String^ keyspace)
        {
            _context = context;
            _keyspace = keyspace;
        }


        void SetKeyspaceContext::Completed(TProtocol^ protocol, Exception^ exception)
        {
            if (exception != nullptr)
            {
                _context->_resultCallback(nullptr, exception);
                return;
            }

            SetKeyspaceResult^ setKeyspaceResult = gcnew SetKeyspaceResult();
            setKeyspaceResult->ReadMessage(protocol);

            if (setKeyspaceResult->Exception != nullptr)
            {
                _context->_resultCallback(nullptr, setKeyspaceResult->Exception);
                return;
            }

            CassandraTransport^ transport = (CassandraTransport^)protocol->Transport;
            transport->_keyspace = _keyspace;
            transport->PrepareWrite();
            transport->_context = _context;
            _context->_args->WriteMessage(protocol);
        }
    }
}