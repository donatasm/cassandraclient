using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading.Tasks;
using Cassandra.Client.Thrift;

namespace Cassandra.Client.Async
{
    public sealed class Ring
    {
        private Ring()
        {
        }

        public static async Task<Ring> DescribeAsync(CassandraClient client, IPEndPoint seedEndPoint, string keyspace)
        {
            var tokenRanges = await client.DescribeRingAsync(new DescribeRingArgs(seedEndPoint, keyspace));
            var addresses = tokenRanges.SelectMany(r => r.Endpoints).Distinct();
            var ringEndPoints = addresses.ToDictionary(address => address, address => ToIPEndPoint(address, seedEndPoint.Port));
            return new Ring
            {
                EndPoints = ringEndPoints.Values,
                TokenRanges = tokenRanges
                    .Select(tokenRange => new TokenRange(
                        tokenRange.Start_token,
                        tokenRange.End_token,
                        tokenRange.Endpoints,
                        ringEndPoints))
            };
        }

        public IEnumerable<EndPoint> EndPoints { get; private set; }
        public IEnumerable<TokenRange> TokenRanges { get; private set; }

        public sealed class TokenRange
        {
            private readonly int _hashCode;

            internal TokenRange(
                string start,
                string end,
                IList<string> addresses,
                IDictionary<string, IPEndPoint> ringEndPoints)
            {
                Start = BigInteger.Parse(start);
                End = BigInteger.Parse(end);
                EndPoints = new IPEndPoint[addresses.Count];

                for (var i = 0; i < addresses.Count; i++)
                {
                    EndPoints[i] = ringEndPoints[addresses[i]];
                }

                unchecked
                {
                    _hashCode = (Start.GetHashCode() * 397) ^ End.GetHashCode();
                }
            }

            public TokenRange(BigInteger start, BigInteger end, IPEndPoint[] endPoints)
            {
                Start = start;
                End = end;
                EndPoints = endPoints;
            }

            public BigInteger Start { get; private set; }
            public BigInteger End { get; private set; }
            public IPEndPoint[] EndPoints { get; private set; }

            public override string ToString()
            {
                return String.Format("{0} - {1}", Start, End);
            }

            private bool Equals(TokenRange other)
            {
                return Start.Equals(other.Start) && End.Equals(other.End);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is TokenRange && Equals((TokenRange)obj);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }

        private static IPEndPoint ToIPEndPoint(string address, int port)
        {
            return new IPEndPoint(IPAddress.Parse(address), port);
        }
    }
}
