using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct NetHeader
{
    public uint ConnId;
    public uint Seq32;
    public ushort Flags;
    public uint ChannelId;
    public fixed byte Reserved[2];
}

