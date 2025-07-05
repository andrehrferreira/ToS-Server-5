using System.Runtime.InteropServices;

[System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential, Size = 32)]
public unsafe struct FixedString32
{
    public fixed byte Buffer[32];

    public void Set(string str)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(str);
        int len = Math.Min(bytes.Length, 31);
        fixed (byte* b = Buffer)
        {
            for (int i = 0; i < len; i++)
                b[i] = bytes[i];

            b[len] = 0;
        }
    }

    public override string ToString()
    {
        fixed (byte* b = Buffer)
        {
            int len = 0;
            while (len < 32 && b[len] != 0) len++;
            return System.Text.Encoding.UTF8.GetString(b, len);
        }
    }
}
