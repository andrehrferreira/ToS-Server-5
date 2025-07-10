/*
* WAFRateLimiter
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using NanoSockets;

public static class WAFRateLimiter
{
    private static readonly ConcurrentDictionary<string, TokenBucket> Buckets = new();

    private const int MaxPacketsPerSecond = 60;
    private const int BurstSize = 5;

    public static bool AllowPacket(Address address)
    {
        string ip = GetIPAddress(address);

        var bucket = Buckets.GetOrAdd(ip, _ => new TokenBucket(MaxPacketsPerSecond, BurstSize));

        return bucket.TryConsume();
    }

    private static string GetIPAddress(Address address)
    {
        StringBuilder ip = new StringBuilder(64);

        if (UDP.GetIP(ref address, ip, 64) == Status.OK)
        {
            return ip.ToString();
        }

        return address.ToString();
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
