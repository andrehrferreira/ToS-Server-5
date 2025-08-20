using System.Buffers.Binary;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;

public unsafe struct SecureSession
{
    private static readonly byte[] Info = System.Text.Encoding.ASCII.GetBytes("ToS-UE5 v1");

    public fixed byte TxKey[32];
    public fixed byte RxKey[32];
    public fixed byte DirSalt[4];
    public ulong SeqTx;
    public ulong SeqRx;

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

        SecureSession sess = default;
        okm.Slice(0, 32).CopyTo(new Span<byte>(sess.TxKey, 32));
        okm.Slice(32, 32).CopyTo(new Span<byte>(sess.RxKey, 32));
        okm.Slice(64, 4).CopyTo(new Span<byte>(sess.DirSalt, 4));
        sess.SeqTx = 0;
        sess.SeqRx = 0;

        return (serverPub, salt, sess);
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

        SecureSession sess = default;
        okm.Slice(32, 32).CopyTo(new Span<byte>(sess.TxKey, 32));
        okm.Slice(0, 32).CopyTo(new Span<byte>(sess.RxKey, 32));
        okm.Slice(64, 4).CopyTo(new Span<byte>(sess.DirSalt, 4));
        sess.SeqTx = 0;
        sess.SeqRx = 0;
        return sess;
    }
}

