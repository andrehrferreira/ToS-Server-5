/*
* PacketHeader
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

using System.Runtime.InteropServices;

[Flags]
public enum PacketHeaderFlags : byte
{
    None = 0,
    Encrypted = 1 << 0,
    AEAD_ChaCha20Poly1305 = 1 << 1,
    Rekey = 1 << 2,
    Fragment = 1 << 3
}

public enum PacketChannel : byte
{
    Unreliable = 0,
    ReliableOrdered = 1,
    ReliableUnordered = 2
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct PacketHeader
{
    public uint ConnectionId;    // 4 bytes LE
    public PacketChannel Channel; // 1 byte
    public PacketHeaderFlags Flags; // 1 byte
    public ulong Sequence;       // 8 bytes LE

    public const int Size = 14;

    public void Serialize(byte* buffer)
    {
        *(uint*)buffer = ConnectionId;
        buffer[4] = (byte)Channel;
        buffer[5] = (byte)Flags;
        *(ulong*)(buffer + 6) = Sequence;
    }

    public static PacketHeader Deserialize(byte* buffer)
    {
        return new PacketHeader
        {
            ConnectionId = *(uint*)buffer,
            Channel = (PacketChannel)buffer[4],
            Flags = (PacketHeaderFlags)buffer[5],
            Sequence = *(ulong*)(buffer + 6)
        };
    }

    public ReadOnlySpan<byte> GetAAD()
    {
        unsafe
        {
            fixed (PacketHeader* ptr = &this)
            {
                return new ReadOnlySpan<byte>(ptr, Size);
            }
        }
    }
}
