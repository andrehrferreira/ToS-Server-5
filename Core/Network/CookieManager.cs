/*
* CookieManager - Anti-spoof cookie system
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

using System.Security.Cryptography;
using NanoSockets;

public static class CookieManager
{
    private static readonly byte[] ServerSecret = new byte[32];
    private static readonly TimeSpan CookieTTL = TimeSpan.FromSeconds(10);

    static CookieManager()
    {
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(ServerSecret);
    }

    public static byte[] GenerateCookie(Address clientAddress)
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

    public static bool ValidateCookie(byte[] cookie, Address clientAddress)
    {
        if (cookie.Length != 48) 
            return false;

        var timestamp = BitConverter.ToInt64(cookie, 0);
        var cookieTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);

        if (DateTimeOffset.UtcNow - cookieTime > CookieTTL)
            return false;

        var random = cookie.AsSpan(8, 8);
        var providedSignature = cookie.AsSpan(16, 32);
        var data = new List<byte>();

        unsafe
        {
            byte* addrPtr = (byte*)&clientAddress;
            data.AddRange(new ReadOnlySpan<byte>(addrPtr, sizeof(Address)));
        }

        data.AddRange(BitConverter.GetBytes(timestamp));
        data.AddRange(random.ToArray());

        using var hmac = new HMACSHA256(ServerSecret);
        var expectedSignature = hmac.ComputeHash(data.ToArray());

        return providedSignature.SequenceEqual(expectedSignature);
    }
}
