using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Apache.Cassandra;
using Thrift.Protocol;
using Thrift.Server;
using Thrift.Transport;

namespace Cassandra.Server
{
    public class CassandraServer : Apache.Cassandra.Cassandra.Iface
    {
        private static long _id;
        private readonly TThreadPoolServer _server;

        public CassandraServer(int port)
        {
            _server = new TThreadPoolServer(
                new Apache.Cassandra.Cassandra.Processor(this),
                new TServerSocket(port),
                new TFramedTransport.Factory(),
                new TFramedTransport.Factory(),
                new TBinaryProtocol.Factory(),
                new TBinaryProtocol.Factory(),
                512, 8192,
                Log);
        }

        public void Start()
        {
            _server.Serve();
        }

        private static void Log(string message)
        {
            LogFormat(message);
        }

        [Conditional("DEBUG")]
        private static void LogFormat(string message, params object[] args)
        {
            Console.WriteLine("[Server Log] {0}", String.Format(message, args));
        }

        private static List<ColumnOrSuperColumn> CreateSuperColumns(int columnCount)
        {
            var id = Interlocked.Increment(ref _id);
            var columns = new List<Column>();

            for (var i = 0; i < columnCount; i++)
            {
                var column = new Column
                {
                    Name = GetBytes("Column#{0}#{1}", id, i + 1),
                    Value = GetBytes("Value#{0}#{1}", id, i + 1),
                    Timestamp = Stopwatch.GetTimestamp(),
                    Ttl = (int)(id % 1000)
                };

                columns.Add(column);
            }

            return columns.Select(c => new ColumnOrSuperColumn
            {
                Super_column = new SuperColumn
                {
                    Name = GetBytes("SuperColumn#{0}", id),
                    Columns = columns
                }
            }).ToList();
        }

        private static byte[] GetBytes(string format, params object[] args)
        {
            return Encoding.UTF8.GetBytes(String.Format(format, args));
        }

        public void set_keyspace(string keyspace)
        {
            LogFormat("Keyspace set to {0}.", keyspace);
        }


        private const string Version = "19.20.20";
        public string describe_version()
        {
            LogFormat("Version is {0}.", Version);
            return Version;
        }

        public List<ColumnOrSuperColumn> get_slice(
            byte[] key,
            ColumnParent column_parent,
            SlicePredicate predicate,
            ConsistencyLevel consistency_level)
        {
            var count = BitConverter.ToInt32(key, 0);
            LogFormat("Key {0}, ColumnParent {1}, ConsistencyLevel {2}, Count {3}.", key, column_parent, consistency_level, count);
            return CreateSuperColumns(count);
        }

        public ColumnOrSuperColumn get(
            byte[] key,
            ColumnPath column_path,
            ConsistencyLevel consistency_level)
        {
            throw new NotImplementedException();
        }

        public void login(AuthenticationRequest auth_request)
        {
            throw new NotImplementedException();
        }

        public int get_count(
            byte[] key,
            ColumnParent column_parent,
            SlicePredicate predicate,
            ConsistencyLevel consistency_level)
        {
            throw new NotImplementedException();
        }

        public Dictionary<byte[], List<ColumnOrSuperColumn>> multiget_slice(
            List<byte[]> keys,
            ColumnParent column_parent,
            SlicePredicate predicate,
            ConsistencyLevel consistency_level)
        {
            throw new NotImplementedException();
        }

        public Dictionary<byte[], int> multiget_count(
            List<byte[]> keys,
            ColumnParent column_parent,
            SlicePredicate predicate,
            ConsistencyLevel consistency_level)
        {
            throw new NotImplementedException();
        }

        public List<KeySlice> get_range_slices(
            ColumnParent column_parent,
            SlicePredicate predicate,
            KeyRange range,
            ConsistencyLevel consistency_level)
        {
            throw new NotImplementedException();
        }

        public List<KeySlice> get_indexed_slices(
            ColumnParent column_parent,
            IndexClause index_clause,
            SlicePredicate column_predicate,
            ConsistencyLevel consistency_level)
        {
            throw new NotImplementedException();
        }

        public void insert(
            byte[] key,
            ColumnParent column_parent,
            Column column,
            ConsistencyLevel consistency_level)
        {
            throw new NotImplementedException();
        }

        public void add(
            byte[] key,
            ColumnParent column_parent,
            CounterColumn column,
            ConsistencyLevel consistency_level)
        {
            throw new NotImplementedException();
        }

        public void remove(
            byte[] key,
            ColumnPath column_path,
            long timestamp,
            ConsistencyLevel consistency_level)
        {
            throw new NotImplementedException();
        }

        public void remove_counter(
            byte[] key,
            ColumnPath path,
            ConsistencyLevel consistency_level)
        {
            throw new NotImplementedException();
        }

        public void batch_mutate(
            Dictionary<byte[], Dictionary<string, List<Mutation>>> mutation_map,
            ConsistencyLevel consistency_level)
        {
            throw new NotImplementedException();
        }

        public void truncate(string cfname)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, List<string>> describe_schema_versions()
        {
            throw new NotImplementedException();
        }

        public List<KsDef> describe_keyspaces()
        {
            throw new NotImplementedException();
        }

        public string describe_cluster_name()
        {
            throw new NotImplementedException();
        }

        public List<TokenRange> describe_ring(string keyspace)
        {
            throw new NotImplementedException();
        }

        public string describe_partitioner()
        {
            throw new NotImplementedException();
        }

        public string describe_snitch()
        {
            throw new NotImplementedException();
        }

        public KsDef describe_keyspace(string keyspace)
        {
            throw new NotImplementedException();
        }

        public List<string> describe_splits(
            string cfName,
            string start_token,
            string end_token,
            int keys_per_split)
        {
            throw new NotImplementedException();
        }

        public string system_add_column_family(CfDef cf_def)
        {
            throw new NotImplementedException();
        }

        public string system_drop_column_family(string column_family)
        {
            throw new NotImplementedException();
        }

        public string system_add_keyspace(KsDef ks_def)
        {
            throw new NotImplementedException();
        }

        public string system_drop_keyspace(string keyspace)
        {
            throw new NotImplementedException();
        }

        public string system_update_keyspace(KsDef ks_def)
        {
            throw new NotImplementedException();
        }

        public string system_update_column_family(CfDef cf_def)
        {
            throw new NotImplementedException();
        }

        public CqlResult execute_cql_query(byte[] query, Compression compression)
        {
            throw new NotImplementedException();
        }
    }
}