using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;

namespace Cassandra.Client.Async
{
    public sealed class KeyRangeBalancer
    {
        private readonly object _syncRoot;
        private readonly Ring.TokenRange[] _tokenRanges;
        private readonly MD5CryptoServiceProvider _md5;
        private readonly IDictionary<Ring.TokenRange, Counter> _counters;

        public KeyRangeBalancer(IEnumerable<Ring.TokenRange> tokenRanges)
        {
            _syncRoot = new object();
            _tokenRanges = tokenRanges.ToArray();
            _md5 = new MD5CryptoServiceProvider();
            _counters = _tokenRanges.ToDictionary(t => t, t => new Counter(t.EndPoints.Length));
        }

        public IPEndPoint GetEndPoint(byte[] key)
        {
            var keyToken = GetKeyToken(key);
            var tokenRange = GetTokenRange(keyToken);
            var index = _counters[tokenRange].Next();
            return tokenRange.EndPoints[index];
        }

        private BigInteger GetKeyToken(byte[] key)
        {
            byte[] hash;

            lock (_syncRoot)
            {
                hash = _md5.ComputeHash(key);
            }

            Array.Reverse(hash);
            return BigInteger.Abs(new BigInteger(hash));
        }

        private Ring.TokenRange GetTokenRange(BigInteger token)
        {
            foreach (var range in _tokenRanges)
            {
                if (range.End > range.Start)
                {
                    if (token > range.Start && token <= range.End)
                    {
                        return range;
                    }
                }
                else
                {
                    if (token > range.Start || token <= range.End)
                    {
                        return range;
                    }
                }
            }

            throw new IndexOutOfRangeException("Token out of range.");
        }

        private sealed class Counter
        {
            private readonly int _maxValue;
            private long _currentValue;

            public Counter(int maxValue)
            {
                _maxValue = maxValue;
                _currentValue = -1;
            }

            public long Next()
            {
                return Interlocked.Increment(ref _currentValue) % _maxValue;
            }
        }
    }
}
