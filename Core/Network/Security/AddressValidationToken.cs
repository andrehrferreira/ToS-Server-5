/*
* AddressValidationToken
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

using System.Security.Cryptography;
using NanoSockets;

namespace Wormhole
{
    public static class AddressValidationToken
    {
        private static readonly byte[] ServerSecret = new byte[32];
        private static readonly TimeSpan TTL = TimeSpan.FromSeconds(10);

        static AddressValidationToken()
        {
            RandomNumberGenerator.Fill(ServerSecret);
        }

        public static byte[] Generate(Address clientAddress)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = new byte[8];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(random);
            var data = new List<byte>();

            unsafe
            {
                byte* addrPtr = (byte*)&clientAddress;
                data.AddRange(new ReadOnlySpan<byte>(addrPtr, sizeof(Address)));
            }

            data.AddRange(BitConverter.GetBytes(timestamp));
            data.AddRange(random);

            using var hmac = new HMACSHA256(ServerSecret);
            var signature = hmac.ComputeHash(data.ToArray());

            var cookie = new byte[8 + 8 + 32];
            BitConverter.GetBytes(timestamp).CopyTo(cookie, 0);
            random.CopyTo(cookie, 8);
            signature.CopyTo(cookie, 16);

            return cookie;
        }

        public static bool Validate(byte[] buffer, Address clientAddress)
        {
            if(buffer == null)
                return false;
            else if (buffer.Length != 48)
                return false;

            var timestamp = BitConverter.ToInt64(buffer, 0);
            var cookieTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);

            if (DateTimeOffset.UtcNow - cookieTime > TTL)
                return false;

            var random = buffer.AsSpan(8, 8);
            var providedSignature = buffer.AsSpan(16, 32);
            var data = new List<byte>();

            unsafe
            {
                byte* addrPtr = (byte*)&clientAddress;
                data.AddRange(new ReadOnlySpan<byte>(addrPtr, sizeof(Address)));
            }

            data.AddRange(BitConverter.GetBytes(timestamp));
            data.AddRange(random.ToArray());

            using var hmac = new HMACSHA256(ServerSecret);
            var expectedSignature = hmac.ComputeHash(data.ToArray()).AsSpan(0, 32).ToArray();

            return providedSignature.SequenceEqual(expectedSignature);
        }
    }
}
