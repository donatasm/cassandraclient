namespace Cassandra.Server
{
    internal static class Run
    {
        private static void Main()
        {
            new CassandraServer(1337).Start();
        }
    }
}
