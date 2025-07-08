public unsafe struct NativeBuffer
{
    public byte* Data;
    public int Length;

    public NativeBuffer(byte* data, int length)
    {
        Data = data;
        Length = length;
    }
}
