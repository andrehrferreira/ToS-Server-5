using System;
using System.IO;

public class IntegrityKeyTable
{
    public string Version { get; set; }
    public short[] Keys { get; set; }
    public int KeyCount => Keys.Length;

    public IntegrityKeyTable(string version, short[] keys)
    {
        Version = version;
        Keys = keys;
    }

    public void SaveToFile(string path)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        bw.Write(Version);
        bw.Write(Keys.Length);

        foreach (var key in Keys)
            bw.Write(key);
    }

    public static IntegrityKeyTable LoadFromFile(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        string version = br.ReadString();
        int len = br.ReadInt32();
        short[] keys = new short[len];

        for (int i = 0; i < len; i++)
            keys[i] = br.ReadInt16();

        return new IntegrityKeyTable(version, keys);
    }
}
