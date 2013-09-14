#include "uv.h"
#using <system.dll>


#define FRAME_HEADER_SIZE 4 // Frame header size
#define MAX_FRAME_SIZE 65536 // Maximum size of a frame including headers


using namespace System;
using namespace System::IO;
using namespace System::Collections::Concurrent;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
using namespace System::Threading;
using namespace Thrift::Protocol;
using namespace Thrift::Transport;
using namespace Cassandra::Client::Thrift;


namespace Cassandra
{
    namespace Client
    {
        public delegate void ResultCallback(TProtocol^ protocol, Exception^ exception);


        ref class CassandraContextQueue;
        ref class CassandraTransport;
        public ref class CassandraClient sealed
        {
        public:
            CassandraClient();
            ~CassandraClient();
            void Send(IArgs^ args, ResultCallback^ resultCallback);
            void Stop();
            void Run();
            CassandraClient^ RunAsync();
        internal:
            initonly CassandraContextQueue^ _contextQueue;
            initonly Queue<CassandraTransport^>^ _transportPool;
        private:
            uv_loop_t* _loop;
            uv_async_t* _notifier;
            uv_async_t* _stop;
            void RunInternal();
        };


        public ref class UvException sealed : Exception
        {
        internal:
            UvException(String^ message);
            static UvException^ CreateFromLastError(int error);
            static void Throw(int error);
        };


        private ref struct CassandraContext sealed
        {
        public:
            CassandraContext(IArgs^ args, ResultCallback^ resultCallback, CassandraClient^ client);
            const char* _address;
            int _port;
            initonly CassandraClient^ _client;
            initonly IArgs^ _args;
            initonly ResultCallback^ _resultCallback;
        };


        private ref class CassandraContextQueue sealed
        {
        public:
            CassandraContextQueue();
            ~CassandraContextQueue();
            void Enqueue(CassandraContext^ context);
            bool TryDequeue(CassandraContext^ %context);
            void* ToPointer();
            static CassandraContextQueue^ FromPointer(void* ptr);
        private:
            GCHandle _handle;
            initonly ConcurrentQueue<CassandraContext^>^ _queue;
        };


        // Socket descriptor with it's buffer
        typedef struct
        {
            uv_tcp_t socket;
            char buffer[MAX_FRAME_SIZE];
        } SocketBuffer;


        public interface class ICassandraTransport
        {
            void Recycle();
            void Close();
        };


        private ref class CassandraTransport sealed : TTransport, ICassandraTransport
        {
        public:
            CassandraTransport(const char* address, int port, uv_loop_t* loop);
            ~CassandraTransport();
            virtual void Open() override;
            virtual void Close() override;
            virtual property bool IsOpen { bool get() override; }
            virtual int Read(array<byte>^ buf, int off, int len) override;
            virtual void Write(array<byte>^ buf, int off, int len) override;
            virtual void Flush() override;
            virtual void Recycle();
            void PrepareWrite();
            void* ToPointer();
            static CassandraTransport^ FromPointer(void* ptr);
            void SendFrame();
            void ReceiveFrame();

            String^ _keyspace;
            SocketBuffer* _socketBuffer;
            CassandraContext^ _context;
            initonly TBinaryProtocol^ _protocol;
            int _header;
            int _position;
            bool _isOpen;

        private:
            const char* _address;
            int _port;
            uv_loop_t* _loop;
            GCHandle _handle;
        };


        private ref class SetKeyspaceContext sealed
        {
        public:
            SetKeyspaceContext(CassandraContext^ context, String^ keyspace);
            void Completed(TProtocol^ protocol, Exception^ exception);
        private:
            initonly CassandraContext^ _context;
            initonly String^ _keyspace;
        };
    }
}
