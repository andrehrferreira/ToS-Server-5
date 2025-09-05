/*
* AddressBlacklist
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using NanoSockets;

namespace Wormhole
{
    public static class AddressBlacklist
    {
        private static readonly ConcurrentDictionary<Address, long> _map =
            new ConcurrentDictionary<Address, long>(Environment.ProcessorCount, 4096);

        private static readonly TimeSpan DefaultTtl = TimeSpan.FromSeconds(30);
        public static int Count => _map.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Ban(Address addr, TimeSpan? ttl = null)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long exp = now + (long)(ttl ?? DefaultTtl).TotalSeconds;

            _map.AddOrUpdate(
                addr,
                static (_, state) => state,
                static (_, __, state) => state,
                exp
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Unban(Address addr) => _map.TryRemove(addr, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBanned(Address addr)
        {
            if (_map.TryGetValue(addr, out long exp))
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (now <= exp) return true;
                _map.TryRemove(addr, out _);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SweepExpired(int budget = 1024)
        {
            int removed = 0;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (var kv in _map)
            {
                if (kv.Value < now)
                {
                    if (_map.TryRemove(kv.Key, out _))
                        removed++;

                    if (removed >= budget) break;
                }
            }
            return removed;
        }
    }
}
