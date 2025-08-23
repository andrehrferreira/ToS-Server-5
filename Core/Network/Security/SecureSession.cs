using System.Buffers.Binary;
using System.Text;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Modes;

public unsafe struct SecureSession
{
    private static readonly byte[] Info = System.Text.Encoding.ASCII.GetBytes("ToS-UE5 v1");

    public fixed byte TxKey[32];    // Server->Client key
    public fixed byte RxKey[32];    // Client->Server key
    public fixed byte SessionSalt[16];
    public ulong SeqTx;
    public ulong SeqRx;
    public uint ConnectionId;

    // Replay protection - sliding window (64 positions)
    private ulong _replayWindow;
    private ulong _highestSeqReceived;

    // Rekey tracking
    public ulong BytesTransmitted;
    public DateTime SessionStartTime;

    private const int ReplayWindowSize = 64;
    private const ulong RekeyBytesThreshold = 1UL << 30; // 1 GB
    private static readonly TimeSpan RekeyTimeThreshold = TimeSpan.FromMinutes(60); // 1 hour

    public static (byte[] serverPublicKey, byte[] salt, SecureSession session) CreateAsServer(ReadOnlySpan<byte> clientPublicKey, uint connectionId)
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

        // Derive 64 bytes: 32 for TxKey (S->C), 32 for RxKey (C->S)
        Span<byte> okm = stackalloc byte[64];
        var hkdf = new HkdfBytesGenerator(new Sha256Digest());
        hkdf.Init(new HkdfParameters(sharedSecret, salt, Info));
        hkdf.GenerateBytes(okm);

        SecureSession sess = default;
        okm.Slice(0, 32).CopyTo(new Span<byte>(sess.TxKey, 32));    // Server->Client
        okm.Slice(32, 32).CopyTo(new Span<byte>(sess.RxKey, 32));   // Client->Server
        salt.CopyTo(new Span<byte>(sess.SessionSalt, 16));
        sess.SeqTx = 0;
        sess.SeqRx = 0;
        sess.ConnectionId = connectionId;
        sess._replayWindow = 0;
        sess._highestSeqReceived = 0;
        sess.BytesTransmitted = 0;
        sess.SessionStartTime = DateTime.UtcNow;

        return (serverPub, salt, sess);
    }

    public static SecureSession CreateAsClient(ReadOnlySpan<byte> clientPrivateKey, ReadOnlySpan<byte> serverPublicKey, ReadOnlySpan<byte> salt, uint connectionId)
    {
        var clientPriv = new X25519PrivateKeyParameters(clientPrivateKey);
        var serverPub = new X25519PublicKeyParameters(serverPublicKey);
        var agreement = new X25519Agreement();
        agreement.Init(clientPriv);
        byte[] sharedSecret = new byte[agreement.AgreementSize];
        agreement.CalculateAgreement(serverPub, sharedSecret, 0);

        Span<byte> okm = stackalloc byte[64];
        var hkdf = new HkdfBytesGenerator(new Sha256Digest());
        hkdf.Init(new HkdfParameters(sharedSecret, salt.ToArray(), Info));
        hkdf.GenerateBytes(okm);

        SecureSession sess = default;
        okm.Slice(32, 32).CopyTo(new Span<byte>(sess.TxKey, 32));   // Client->Server (reversed from server perspective)
        okm.Slice(0, 32).CopyTo(new Span<byte>(sess.RxKey, 32));    // Server->Client
        salt.CopyTo(new Span<byte>(sess.SessionSalt, 16));
        sess.SeqTx = 0;
        sess.SeqRx = 0;
        sess.ConnectionId = connectionId;
        sess._replayWindow = 0;
        sess._highestSeqReceived = 0;
        sess.BytesTransmitted = 0;
        sess.SessionStartTime = DateTime.UtcNow;
        return sess;
    }

    // Generate 12-byte nonce: conn_id (4B LE) || seq (8B LE)
    public void GenerateNonce(ulong sequence, Span<byte> nonce)
    {
        if (nonce.Length < 12)
            throw new ArgumentException("Nonce must be at least 12 bytes");

        BinaryPrimitives.WriteUInt32LittleEndian(nonce, ConnectionId);
        BinaryPrimitives.WriteUInt64LittleEndian(nonce.Slice(4), sequence);
    }

        // Encrypt payload with AEAD ChaCha20-Poly1305
    public bool EncryptPayload(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad, Span<byte> ciphertext, out int ciphertextLength)
    {
        try
        {
            Span<byte> nonce = stackalloc byte[12];
            GenerateNonce(SeqTx, nonce);

            var cipher = new ChaCha20Poly1305();

            unsafe
            {
                fixed (byte* txKeyPtr = TxKey)
                {
                    var keyParam = new KeyParameter(new ReadOnlySpan<byte>(txKeyPtr, 32).ToArray());
                    var parameters = new AeadParameters(keyParam, 128, nonce.ToArray(), aad.ToArray());

                    cipher.Init(true, parameters);
                    ciphertextLength = cipher.ProcessBytes(plaintext.ToArray(), 0, plaintext.Length, ciphertext.ToArray(), 0);
                    ciphertextLength += cipher.DoFinal(ciphertext.Slice(ciphertextLength).ToArray(), 0);
                }
            }

            SeqTx++;
            BytesTransmitted += (ulong)ciphertextLength;
            return true;
        }
        catch
        {
            ciphertextLength = 0;
            return false;
        }
    }

        // Decrypt payload with AEAD ChaCha20-Poly1305 and replay protection
    public bool DecryptPayload(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> aad, ulong sequence, Span<byte> plaintext, out int plaintextLength)
    {
        try
        {
            // Check for replay attack
            if (!IsSequenceValid(sequence))
            {
                plaintextLength = 0;
                return false;
            }

            Span<byte> nonce = stackalloc byte[12];
            BinaryPrimitives.WriteUInt32LittleEndian(nonce, ConnectionId);
            BinaryPrimitives.WriteUInt64LittleEndian(nonce.Slice(4), sequence);

            var cipher = new ChaCha20Poly1305();

            unsafe
            {
                fixed (byte* rxKeyPtr = RxKey)
                {
                    var keyParam = new KeyParameter(new ReadOnlySpan<byte>(rxKeyPtr, 32).ToArray());
                    var parameters = new AeadParameters(keyParam, 128, nonce.ToArray(), aad.ToArray());

                    cipher.Init(false, parameters);
                    plaintextLength = cipher.ProcessBytes(ciphertext.ToArray(), 0, ciphertext.Length, plaintext.ToArray(), 0);
                    plaintextLength += cipher.DoFinal(plaintext.Slice(plaintextLength).ToArray(), 0);
                }
            }

            // Update replay protection window
            UpdateReplayWindow(sequence);
            return true;
        }
        catch
        {
            plaintextLength = 0;
            return false;
        }
    }

    // Replay protection with sliding window
    private bool IsSequenceValid(ulong sequence)
    {
        // Accept if sequence is higher than any we've seen
        if (sequence > _highestSeqReceived)
            return true;

        // Reject if too old (outside window)
        if (sequence + ReplayWindowSize <= _highestSeqReceived)
            return false;

        // Check if we've already seen this sequence number
        ulong diff = _highestSeqReceived - sequence;
        if (diff < ReplayWindowSize)
        {
            ulong mask = 1UL << (int)diff;
            return (_replayWindow & mask) == 0;
        }

        return false;
    }

    private void UpdateReplayWindow(ulong sequence)
    {
        if (sequence > _highestSeqReceived)
        {
            // Shift window forward
            ulong shift = sequence - _highestSeqReceived;
            if (shift >= ReplayWindowSize)
            {
                _replayWindow = 1; // Only the current bit set
            }
            else
            {
                _replayWindow = (_replayWindow << (int)shift) | 1;
            }
            _highestSeqReceived = sequence;
        }
        else
        {
            // Mark this sequence as seen
            ulong diff = _highestSeqReceived - sequence;
            if (diff < ReplayWindowSize)
            {
                ulong mask = 1UL << (int)diff;
                _replayWindow |= mask;
            }
        }
    }

    // Check if rekey is needed based on bytes transmitted or time elapsed
    public bool ShouldRekey()
    {
        return BytesTransmitted >= RekeyBytesThreshold ||
               (DateTime.UtcNow - SessionStartTime) >= RekeyTimeThreshold;
    }

    // Perform rekey operation - derive new keys from current session
    public bool PerformRekey()
    {
        try
        {
            // Create rekey context: "rekey" || old_seq_max
            var rekeyContext = new List<byte>();
            rekeyContext.AddRange(Encoding.ASCII.GetBytes("rekey"));
            rekeyContext.AddRange(BitConverter.GetBytes(Math.Max(SeqTx, SeqRx)));

                        // Derive new keys using HKDF with current session salt and rekey context
            Span<byte> okm = stackalloc byte[64];
            var hkdf = new HkdfBytesGenerator(new Sha256Digest());

            unsafe
            {
                fixed (byte* saltPtr = SessionSalt)
                {
                    hkdf.Init(new HkdfParameters(new ReadOnlySpan<byte>(saltPtr, 16).ToArray(), null, rekeyContext.ToArray()));
                    hkdf.GenerateBytes(okm);

                    // Update keys
                    fixed (byte* txKeyPtr = TxKey)
                    fixed (byte* rxKeyPtr = RxKey)
                    {
                        okm.Slice(0, 32).CopyTo(new Span<byte>(txKeyPtr, 32));
                        okm.Slice(32, 32).CopyTo(new Span<byte>(rxKeyPtr, 32));
                    }
                }
            }

            // Reset counters
            BytesTransmitted = 0;
            SessionStartTime = DateTime.UtcNow;

            // Reset sequence numbers to prevent confusion
            SeqTx = 0;
            SeqRx = 0;
            _replayWindow = 0;
            _highestSeqReceived = 0;

            return true;
        }
        catch
        {
            return false;
        }
    }
}

