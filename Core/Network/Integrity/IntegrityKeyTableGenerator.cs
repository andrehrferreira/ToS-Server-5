using System.Security.Cryptography;

public static class IntegrityKeyTableGenerator
{
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

    public static IntegrityKeyTable GenerateTable(string version)
    {
        short[] keys = new short[2048];

        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] buffer = new byte[2];

            for (int i = 0; i < keys.Length; i++)
            {
                rng.GetBytes(buffer);
                short value = BitConverter.ToInt16(buffer, 0);
                keys[i] = (short)(value & 0x7FFF); 
            }
        }

        return new IntegrityKeyTable(version, keys);
    }

    public static ushort GenerateIndex()
    {
        byte[] buffer = new byte[2];
        ushort index;

        do
        {
            _rng.GetBytes(buffer);
            index = (ushort)(BitConverter.ToUInt16(buffer, 0) & 0x7FF);
        }
        while (index > 2047);

        return index;
    }
}
