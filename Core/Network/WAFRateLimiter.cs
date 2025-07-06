using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

public static class WAFRateLimiter
{
    private static readonly ConcurrentDictionary<string, TokenBucket> Buckets = new();

    private const int MaxPacketsPerSecond = 60;
    private const int BurstSize = 5;

    public static bool AllowPacket(EndPoint endPoint)
    {
        string ip = GetIPAddress(endPoint);

        var bucket = Buckets.GetOrAdd(ip, _ => new TokenBucket(MaxPacketsPerSecond, BurstSize));

        return bucket.TryConsume();
    }

    private static string GetIPAddress(EndPoint endPoint)
    {
        if (endPoint is IPEndPoint ipEndPoint)
            return ipEndPoint.Address.ToString();

        return endPoint.ToString();
    }

    private class TokenBucket
    {
        private readonly int _rate;
        private readonly int _burst;
        private double _tokens;
        private long _lastRefill;

        private readonly object _lock = new();

        public TokenBucket(int rate, int burst)
        {
            _rate = rate;
            _burst = burst;
            _tokens = rate + burst;
            _lastRefill = Stopwatch.GetTimestamp();
        }

        public bool TryConsume()
        {
            lock (_lock)
            {
                RefillTokens();

                if (_tokens >= 1)
                {
                    _tokens -= 1;
                    return true;
                }

                return false;
            }
        }

        private void RefillTokens()
        {
            long now = Stopwatch.GetTimestamp();
            double elapsedSeconds = (now - _lastRefill) / (double)Stopwatch.Frequency;

            double tokensToAdd = elapsedSeconds * _rate;
            if (tokensToAdd > 0)
            {
                _tokens = Math.Min(_tokens + tokensToAdd, _rate + _burst);
                _lastRefill = now;
            }
        }
    }
}
