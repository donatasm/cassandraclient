#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        UvException::UvException(String^ message) : Exception(message)
        {
        }


        UvException^ UvException::CreateFrom(int error)
        {
            return gcnew UvException(gcnew String(uv_strerror(error)));
        }


        void UvException::Throw(int error)
        {
            throw CreateFrom(error);
        }
    }
}