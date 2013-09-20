#include "CassandraClient.h"

namespace Cassandra
{
    namespace Client
    {
        bool TransportKeyspaceIsNotSet(CassandraTransport^ transport, IArgs^ args)
        {
            return args->Keyspace != nullptr && transport->_keyspace != args->Keyspace;
        }

        void NotifyCompleted(uv_async_t* notifier, int status)
        {
            CassandraContextQueue^ contextQueue = CassandraContextQueue::FromPointer(notifier->data);
            CassandraContext^ context;

            while (contextQueue->TryDequeue(context))
            {
                IPEndPoint^ endPoint = context->_args->EndPoint;

                CassandraClient^ client = context->_client;

                // try get transport from a pool
                CassandraTransport^ transport = (CassandraTransport^)client->_transportPool->Get(endPoint);

                // or try create new if no pooled transports are available
                if (transport == nullptr)
                {
                    transport = client->_factory->CreateTransport(endPoint);

                    // transport limit reached
                    if (transport == nullptr)
                    {
                        context->_resultCallback(nullptr, gcnew TTransportException("Transport limit reached."));
                        continue;
                    }
                }

                // check if keyspace needs to be set
                if (TransportKeyspaceIsNotSet(transport, context->_args))
                {
                    // send set keyspace args before the original args is sent

                    String^ keyspace = context->_args->Keyspace;
                    SetKeyspaceArgs^ setKeyspaceArgs = gcnew SetKeyspaceArgs(endPoint, keyspace);
                    SetKeyspaceContext^ setKeyspaceContext = gcnew SetKeyspaceContext(context, keyspace);
                    ResultCallback^ setKeyspaceCompleted = gcnew ResultCallback(setKeyspaceContext, &SetKeyspaceContext::Completed);
                    transport->_context = gcnew CassandraContext(setKeyspaceArgs, setKeyspaceCompleted, client);
                }
                else
                {
                    transport->_context = context;
                }

                transport->_context->_args->WriteMessage(transport->_protocol);
            }
        }


        void StopCompleted(uv_async_t* stop, int status)
        {
            uv_stop(stop->loop);
        }


        CassandraClient::CassandraClient(int maxEndPointTransportCount)
        {
            _loop = uv_loop_new();

            _contextQueue = gcnew CassandraContextQueue();
            _transportPool = gcnew CassandraTransportPool();
            _factory = gcnew CassandraTransport::Factory(maxEndPointTransportCount, _loop);

            _notifier = new uv_async_t();
            _notifier->data = _contextQueue->ToPointer();
            uv_async_init(_loop, _notifier, NotifyCompleted);

            _stop = new uv_async_t();
            uv_async_init(_loop, _stop, StopCompleted);
        }


        CassandraClient::~CassandraClient()
        {
            uv_loop_delete(_loop);

            delete _notifier;
            delete _stop;
            delete _contextQueue;

            for each(IPEndPoint^ endPoint in _transportPool->GetEndPoints())
            {
                while (true)
                {
                    ICassandraTransport^ transport = _transportPool->Get(endPoint);

                    if (transport == nullptr)
                    {
                        break;
                    }

                    transport->Close();
                }
            }
        }


        void CassandraClient::Stop()
        {
            uv_async_send(_stop);
        }


        void CassandraClient::Send(IArgs^ args, ResultCallback^ resultCallback)
        {
            if (args == nullptr)
                throw gcnew ArgumentNullException("args");

            if (resultCallback == nullptr)
                throw gcnew ArgumentNullException("resultCallback");

            CassandraContext^ context = gcnew CassandraContext(args, resultCallback, this);

            _contextQueue->Enqueue(context);

            // notify that there are requests pending
            uv_async_send(_notifier);
        }


        void CassandraClient::Run()
        {
            uv_run(_loop, UV_RUN_DEFAULT);
        }


        CassandraClient^ CassandraClient::RunAsync()
        {
            Thread^ loopThread = gcnew Thread(gcnew ThreadStart(this, &CassandraClient::RunInternal));
            loopThread->IsBackground = true;
            loopThread->Start();
            return this;
        }


        void CassandraClient::RunInternal()
        {
            this->Run();
            delete this;
        }
    }
}