#include "uv.h"
#using <system.dll>


#define FRAME_HEADER_SIZE 4 // Frame header size
#define MAX_FRAME_SIZE 65536 // Maximum size of a frame including headers


using namespace System;
using namespace System::IO;
using namespace System::Collections::Concurrent;
using namespace System::Collections::Generic;
using namespace System::Net;
using namespace System::Runtime::CompilerServices;
using namespace System::Runtime::InteropServices;
using namespace System::Threading;
using namespace Thrift::Protocol;
using namespace Thrift::Transport;
using namespace Cassandra::Client::Thrift;

[assembly:InternalsVisibleTo("Cassandra.Client.Test")];

namespace Cassandra
{
    namespace Client
    {
        public delegate void ResultCallback(TProtocol^ protocol, Exception^ exception);


        ref class CassandraClient;


        private ref class CassandraContext sealed
        {
        public:
            CassandraContext(IArgs^ args, ResultCallback^ resultCallback, CassandraClient^ client);
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
            property IPEndPoint^ EndPoint { IPEndPoint^ get(); }
            void Recycle();
            void Close();
        };


        private ref class CassandraTransport sealed : TTransport, ICassandraTransport
        {
        public:
            ~CassandraTransport();
            virtual void Open() override;
            virtual void Close() override;
            virtual property bool IsOpen { bool get() override; }
            virtual int Read(array<byte>^ buf, int off, int len) override;
            virtual void Write(array<byte>^ buf, int off, int len) override;
            virtual void Flush() override;
            virtual void Recycle();
            virtual property IPEndPoint^ EndPoint { IPEndPoint^ get(); }
            void PrepareWrite();
            void* ToPointer();
            static CassandraTransport^ FromPointer(void* ptr);
            void SendFrame();
            void ReceiveFrame();
            void SetError(Exception^ exception);
            void SetError(int error);

            String^ _keyspace;
            SocketBuffer* _socketBuffer;
            CassandraContext^ _context;
            initonly TBinaryProtocol^ _protocol;
            int _header;
            int _position;
            bool _isOpen;

            ref class Factory
            {
            public:
                Factory(int maxEndPointTransportCount, uv_loop_t* loop);
                CassandraTransport^ CreateTransport(IPEndPoint^ endPoint);
                void CloseTransport(IPEndPoint^ endPoint);
            private:
                uv_loop_t* _loop;
                initonly int _maxEndPointTransportCount;
                initonly Dictionary<IPEndPoint^, int>^ _endPointTransportCount;
            };

        private:
            CassandraTransport(IPEndPoint^ endPoint, CassandraTransport::Factory^ factory, uv_loop_t* loop);

            const char* _address;
            int _port;
            uv_loop_t* _loop;
            GCHandle _handle;
            initonly IPEndPoint^ _endPoint;
            initonly CassandraTransport::Factory^ _factory;
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


        private ref class CassandraTransportPool
        {
        public:
            CassandraTransportPool();
            void Add(ICassandraTransport^ transport);
            ICassandraTransport^ CassandraTransportPool::Get(IPEndPoint^ endPoint);
            IEnumerable<IPEndPoint^>^ GetEndPoints();
        private:
            Dictionary<IPEndPoint^, Queue<ICassandraTransport^>^>^ _pool;
        };


        public ref class CassandraClient sealed
        {
        public:
            CassandraClient(int maxEndPointTransportCount);
            ~CassandraClient();
            void Send(IArgs^ args, ResultCallback^ resultCallback);
            void Stop();
            void Run();
            CassandraClient^ RunAsync();
        internal:
            initonly CassandraContextQueue^ _contextQueue;
            initonly CassandraTransportPool^ _transportPool;
            initonly CassandraTransport::Factory^ _factory;
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
            static UvException^ CreateFrom(int error);
            static void Throw(int error);
        };
    }
}
