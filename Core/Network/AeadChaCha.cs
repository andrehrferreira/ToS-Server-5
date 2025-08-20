using System;
using System.Buffers;
using System.Buffers.Binary;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto;
using System.Runtime.CompilerServices;

public static unsafe class AeadChaCha
{
    private const int MacBits = 128;           // 16 bytes * 8
    private const int MacSize = 16;
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int HeaderSize = 16;         // sizeof(NetHeader)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopyHeader(in NetHeader hdr, byte[] dest)
    {
        if (dest.Length < HeaderSize) throw new ArgumentException("Header dest too small");
        fixed (NetHeader* p = &hdr)
        {
            new ReadOnlySpan<byte>((byte*)p, HeaderSize).CopyTo(dest);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadHeaderSeq(in NetHeader hdr)
    {
        fixed (NetHeader* p = &hdr)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(new ReadOnlySpan<byte>(((byte*)p) + 4, 4));
        }
    }

    public static bool Seal(SecureSession* s, in NetHeader hdr,
                            byte* plain, int plainLen,
                            byte* outBuf, int outCap, out int outLen)
    {
        outLen = 0;
        if (s == null || plain == null || outBuf == null || plainLen < 0) return false;

        int need = plainLen + MacSize;
        if (outCap < need) return false;

        byte[] keyArr = ArrayPool<byte>.Shared.Rent(KeySize);
        byte[] nonceArr = ArrayPool<byte>.Shared.Rent(NonceSize);
        byte[] aadArr = ArrayPool<byte>.Shared.Rent(HeaderSize);
        byte[] output = ArrayPool<byte>.Shared.Rent(need);
        byte[] plainArr = ArrayPool<byte>.Shared.Rent(plainLen);

        try
        {
            new Span<byte>(s->TxKey, KeySize).CopyTo(keyArr);
            new Span<byte>(s->DirSalt, 4).CopyTo(nonceArr);
            BinaryPrimitives.WriteUInt64BigEndian(nonceArr.AsSpan(4), s->SeqTx);
            CopyHeader(in hdr, aadArr);
            new Span<byte>(plain, plainLen).CopyTo(plainArr);

            var parameters = new AeadParameters(new KeyParameter(keyArr, 0, KeySize), MacBits, nonceArr, aadArr);
            var cipher = new ChaCha20Poly1305();
            cipher.Init(true, parameters);

            int ctLen = cipher.ProcessBytes(plainArr, 0, plainLen, output, 0);
            ctLen += cipher.DoFinal(output, ctLen);
            if (ctLen != need) return false;

            new Span<byte>(output, 0, need).CopyTo(new Span<byte>(outBuf, need));
            outLen = need;
            s->SeqTx++;
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            ClearAndReturn(keyArr);
            ClearAndReturn(nonceArr);
            ClearAndReturn(aadArr);
            ClearAndReturn(plainArr, plainLen);
            ClearAndReturn(output, need);
        }
    }

    public static bool Open(SecureSession* s, in NetHeader hdr,
                            byte* cipherText, int cipherLen,
                            byte* outBuf, int outCap, out int outLen)
    {
        outLen = 0;
        if (s == null || cipherText == null || outBuf == null || cipherLen < MacSize) return false;

        int plainLen = cipherLen - MacSize;
        if (outCap < plainLen) return false;

        if ((uint)s->SeqRx != ReadHeaderSeq(in hdr)) return false;

        byte[] keyArr = ArrayPool<byte>.Shared.Rent(KeySize);
        byte[] nonceArr = ArrayPool<byte>.Shared.Rent(NonceSize);
        byte[] aadArr = ArrayPool<byte>.Shared.Rent(HeaderSize);
        byte[] cipherArr = ArrayPool<byte>.Shared.Rent(cipherLen);
        byte[] output = ArrayPool<byte>.Shared.Rent(plainLen);

        try
        {
            new Span<byte>(s->RxKey, KeySize).CopyTo(keyArr);
            new Span<byte>(s->DirSalt, 4).CopyTo(nonceArr);
            BinaryPrimitives.WriteUInt64BigEndian(nonceArr.AsSpan(4), s->SeqRx);
            CopyHeader(in hdr, aadArr);
            new Span<byte>(cipherText, cipherLen).CopyTo(cipherArr);

            var parameters = new AeadParameters(new KeyParameter(keyArr, 0, KeySize), MacBits, nonceArr, aadArr);
            var cipher = new ChaCha20Poly1305();
            cipher.Init(false, parameters);

            int outLenTemp = cipher.ProcessBytes(cipherArr, 0, cipherLen, output, 0);
            try
            {
                outLenTemp += cipher.DoFinal(output, outLenTemp);
            }
            catch (InvalidCipherTextException)
            {
                return false;
            }

            if (outLenTemp != plainLen) return false;

            new Span<byte>(output, 0, plainLen).CopyTo(new Span<byte>(outBuf, plainLen));
            outLen = plainLen;
            s->SeqRx++;
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            ClearAndReturn(keyArr);
            ClearAndReturn(nonceArr);
            ClearAndReturn(aadArr);
            ClearAndReturn(cipherArr, cipherLen);
            ClearAndReturn(output, plainLen);
        }
    }

    private static void ClearAndReturn(byte[] arr, int length = -1)
    {
        if (arr == null) return;
        if (length < 0 || length > arr.Length) length = arr.Length;
        Array.Clear(arr, 0, length);
        ArrayPool<byte>.Shared.Return(arr);
    }
}

