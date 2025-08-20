using System.Buffers.Binary;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto;

public static unsafe class AeadChaCha
{
    public static bool Seal(SecureSession* s, in NetHeader hdr,
                            byte* plain, int plainLen,
                            byte* outBuf, int outCap, out int outLen)
    {
        outLen = 0;
        int need = plainLen + 16;
        if (outCap < need)
            return false;

        Span<byte> key = stackalloc byte[32];
        new Span<byte>(s->TxKey, 32).CopyTo(key);
        Span<byte> nonce = stackalloc byte[12];
        new Span<byte>(s->DirSalt, 4).CopyTo(nonce);
        BinaryPrimitives.WriteUInt64BigEndian(nonce.Slice(4), s->SeqTx);

        Span<byte> aad = new Span<byte>((byte*)&hdr, sizeof(NetHeader));
        byte[] keyArr = key.ToArray();
        byte[] nonceArr = nonce.ToArray();
        byte[] aadArr = aad.ToArray();
        byte[] plainArr = new byte[plainLen];
        new Span<byte>(plain, plainLen).CopyTo(plainArr);

        var parameters = new AeadParameters(new KeyParameter(keyArr), 128, nonceArr, aadArr);
        var cipher = new ChaCha20Poly1305();
        cipher.Init(true, parameters);
        byte[] output = new byte[need];
        int len = cipher.ProcessBytes(plainArr, 0, plainLen, output, 0);
        cipher.DoFinal(output, len);

        new Span<byte>(output).CopyTo(new Span<byte>(outBuf, need));
        outLen = need;
        s->SeqTx++;
        return true;
    }

    public static bool Open(SecureSession* s, in NetHeader hdr,
                            byte* cipherText, int cipherLen,
                            byte* outBuf, int outCap, out int outLen)
    {
        outLen = 0;
        if (cipherLen < 16)
            return false;
        int need = cipherLen - 16;
        if (outCap < need)
            return false;
        if ((uint)s->SeqRx != BinaryPrimitives.ReadUInt32BigEndian(((byte*)&hdr) + 4))
            return false;

        Span<byte> key = stackalloc byte[32];
        new Span<byte>(s->RxKey, 32).CopyTo(key);
        Span<byte> nonce = stackalloc byte[12];
        new Span<byte>(s->DirSalt, 4).CopyTo(nonce);
        BinaryPrimitives.WriteUInt64BigEndian(nonce.Slice(4), s->SeqRx);
        Span<byte> aad = new Span<byte>((byte*)&hdr, sizeof(NetHeader));

        byte[] keyArr = key.ToArray();
        byte[] nonceArr = nonce.ToArray();
        byte[] aadArr = aad.ToArray();
        byte[] cipherArr = new byte[cipherLen];
        new Span<byte>(cipherText, cipherLen).CopyTo(cipherArr);

        var parameters = new AeadParameters(new KeyParameter(keyArr), 128, nonceArr, aadArr);
        var cipher = new ChaCha20Poly1305();
        cipher.Init(false, parameters);
        byte[] output = new byte[need];
        int len = cipher.ProcessBytes(cipherArr, 0, cipherLen, output, 0);
        try
        {
            cipher.DoFinal(output, len);
        }
        catch (InvalidCipherTextException)
        {
            return false;
        }

        new Span<byte>(output).CopyTo(new Span<byte>(outBuf, need));
        outLen = need;
        s->SeqRx++;
        return true;
    }
}

