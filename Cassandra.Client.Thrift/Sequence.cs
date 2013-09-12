namespace Cassandra.Client.Thrift
{
    internal static class Sequence
    {
        private const int _sequence = 0;

        public static int GetId()
        {
            return _sequence;
        }
    }
}
