using System.Buffers.Binary;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;

public readonly struct Header
{
    public readonly uint ConnId;
    public readonly ulong Seq;
    public readonly PacketFlags Flags;
    public readonly byte ChannelId;

    public Header(uint connId, ulong seq, PacketFlags flags, byte channelId)
    {
        ConnId = connId;
        Seq = seq;
        Flags = flags;
        ChannelId = channelId;
    }

    public byte[] ToArray()
    {
        Span<byte> buf = stackalloc byte[14];
        BinaryPrimitives.WriteUInt32BigEndian(buf, ConnId);
        BinaryPrimitives.WriteUInt64BigEndian(buf.Slice(4), Seq);
        buf[12] = (byte)Flags;
        buf[13] = ChannelId;
        return buf.ToArray();
    }
}

public sealed class SecureSession
{
    private static readonly byte[] Info = System.Text.Encoding.ASCII.GetBytes("ToS-UE5 v1");

    public readonly byte[] TxKey;
    public readonly byte[] RxKey;
    public readonly byte[] DirSalt;

    public ulong SeqTx;
    public ulong SeqRx;

    private SecureSession(byte[] txKey, byte[] rxKey, byte[] dirSalt)
    {
        TxKey = txKey;
        RxKey = rxKey;
        DirSalt = dirSalt;
        SeqTx = 0;
        SeqRx = 0;
    }

    public static (byte[] serverPublicKey, byte[] salt, SecureSession session) CreateAsServer(ReadOnlySpan<byte> clientPublicKey)
    {
        var rng = new SecureRandom();
        var serverPriv = new X25519PrivateKeyParameters(rng);
        var serverPub = serverPriv.GeneratePublicKey().GetEncoded();

        var clientPub = new X25519PublicKeyParameters(clientPublicKey);
        var agreement = new X25519Agreement();
        agreement.Init(serverPriv);
        byte[] sharedSecret = new byte[agreement.AgreementSize];
        agreement.CalculateAgreement(clientPub, sharedSecret, 0);

        var salt = new byte[16];
        rng.NextBytes(salt);

        Span<byte> okm = stackalloc byte[68];
        var hkdf = new HkdfBytesGenerator(new Sha256Digest());
        hkdf.Init(new HkdfParameters(sharedSecret, salt, Info));
        hkdf.GenerateBytes(okm);

        var txKey = okm.Slice(0, 32).ToArray();
        var rxKey = okm.Slice(32, 32).ToArray();
        var dirSalt = okm.Slice(64, 4).ToArray();

        return (serverPub, salt, new SecureSession(txKey, rxKey, dirSalt));
    }

    public static SecureSession CreateAsClient(ReadOnlySpan<byte> clientPrivateKey, ReadOnlySpan<byte> serverPublicKey, ReadOnlySpan<byte> salt)
    {
        var clientPriv = new X25519PrivateKeyParameters(clientPrivateKey);
        var serverPub = new X25519PublicKeyParameters(serverPublicKey);
        var agreement = new X25519Agreement();
        agreement.Init(clientPriv);
        byte[] sharedSecret = new byte[agreement.AgreementSize];
        agreement.CalculateAgreement(serverPub, sharedSecret, 0);

        Span<byte> okm = stackalloc byte[68];
        var hkdf = new HkdfBytesGenerator(new Sha256Digest());
        hkdf.Init(new HkdfParameters(sharedSecret, salt.ToArray(), Info));
        hkdf.GenerateBytes(okm);

        var rxKey = okm.Slice(0, 32).ToArray();
        var txKey = okm.Slice(32, 32).ToArray();
        var dirSalt = okm.Slice(64, 4).ToArray();

        return new SecureSession(txKey, rxKey, dirSalt);
    }

    public byte[] EncryptPacket(ref Header header, ReadOnlySpan<byte> plain)
    {
        header = new Header(header.ConnId, SeqTx, PacketFlagsUtils.AddFlag(header.Flags, PacketFlags.Encrypted), header.ChannelId);

        Span<byte> nonce = stackalloc byte[12];
        DirSalt.CopyTo(nonce);
        BinaryPrimitives.WriteUInt64BigEndian(nonce.Slice(4), SeqTx);

        var parameters = new AeadParameters(new KeyParameter(TxKey), 128, nonce.ToArray(), header.ToArray());
        var cipher = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();
        cipher.Init(true, parameters);

        byte[] output = new byte[plain.Length + 16];
        int len = cipher.ProcessBytes(plain.ToArray(), 0, plain.Length, output, 0);
        cipher.DoFinal(output, len);

        SeqTx++;
        return output;
    }

    public byte[] DecryptPacket(Header header, ReadOnlySpan<byte> cipherText)
    {
        if (header.Seq != SeqRx)
            throw new InvalidOperationException("Invalid sequence");

        Span<byte> nonce = stackalloc byte[12];
        DirSalt.CopyTo(nonce);
        BinaryPrimitives.WriteUInt64BigEndian(nonce.Slice(4), SeqRx);

        var parameters = new AeadParameters(new KeyParameter(RxKey), 128, nonce.ToArray(), header.ToArray());
        var cipher = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();
        cipher.Init(false, parameters);

        byte[] output = new byte[cipherText.Length - 16];
        int len = cipher.ProcessBytes(cipherText.ToArray(), 0, cipherText.Length, output, 0);
        try
        {
            cipher.DoFinal(output, len);
        }
        catch (InvalidCipherTextException e)
        {
            throw new InvalidOperationException("Invalid authentication tag", e);
        }

        SeqRx++;
        return output;
    }
}

