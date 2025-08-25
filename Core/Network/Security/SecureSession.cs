using System.Buffers.Binary;
using System.Text;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Macs;

public unsafe struct SecureSession
{
    private static readonly byte[] Info = System.Text.Encoding.ASCII.GetBytes("ToS-UE5 v1");

    public fixed byte TxKey[32];
    public fixed byte RxKey[32];
    public fixed byte SessionSalt[16];
    public ulong SeqTx;
    public ulong SeqRx;
    public uint ConnectionId;

    public readonly string GetTxKeyHex()
    {
        unsafe
        {
            fixed (byte* ptr = TxKey)
            {
                return Convert.ToHexString(new ReadOnlySpan<byte>(ptr, 32));
            }
        }
    }

    public readonly string GetRxKeyHex()
    {
        unsafe
        {
            fixed (byte* ptr = RxKey)
            {
                return Convert.ToHexString(new ReadOnlySpan<byte>(ptr, 32));
            }
        }
    }

    private ulong _replayWindow;
    private ulong _highestSeqReceived;

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

        Span<byte> okm = stackalloc byte[64];
        var hkdf = new HkdfBytesGenerator(new Sha256Digest());
        hkdf.Init(new HkdfParameters(sharedSecret, salt, Info));
        hkdf.GenerateBytes(okm);

        SecureSession sess = default;
        okm.Slice(0, 32).CopyTo(new Span<byte>(sess.TxKey, 32));
        okm.Slice(32, 32).CopyTo(new Span<byte>(sess.RxKey, 32));
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

    public void GenerateNonce(ulong sequence, Span<byte> nonce)
    {
        if (nonce.Length < 12)
            throw new ArgumentException("Nonce must be at least 12 bytes");

        BinaryPrimitives.WriteUInt32LittleEndian(nonce, ConnectionId);
        BinaryPrimitives.WriteUInt64LittleEndian(nonce.Slice(4), sequence);
    }

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
        catch (Exception ex)
        {
#if DEBUG
            ServerMonitor.Log($"[CRYPTO] Encrypt failed: {ex.Message}");
#endif
            ciphertextLength = 0;
            return false;
        }
    }

    public bool EncryptPayloadWithCompression(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad, Span<byte> result, out int resultLength, out bool wasCompressed)
    {
        try
        {
            wasCompressed = false;
            resultLength = 0;

            Span<byte> encrypted = stackalloc byte[plaintext.Length + 16];
            if (!EncryptPayload(plaintext, aad, encrypted, out int encryptedLength))
                return false;

            if (encryptedLength > 512)
            {
                unsafe
                {
                    fixed (byte* encryptedPtr = encrypted)
                    fixed (byte* resultPtr = result)
                    {
                        int compressedLength = LZ4.Compress(encryptedPtr, encryptedLength, resultPtr, result.Length);
                        if (compressedLength > 0 && compressedLength < encryptedLength)
                        {
                            wasCompressed = true;
                            resultLength = compressedLength;
                            return true;
                        }
                    }
                }
            }

            if (encryptedLength <= result.Length)
            {
                encrypted.Slice(0, encryptedLength).CopyTo(result);
                resultLength = encryptedLength;
                return true;
            }

            return false;
        }
        catch
        {
            wasCompressed = false;
            resultLength = 0;
            return false;
        }
    }

    public bool EncryptPayloadWithSpecificSequence(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad, ulong sequence, Span<byte> ciphertext, out int ciphertextLength)
    {
        try
        {
            // Use specific sequence for nonce generation
            Span<byte> nonce = stackalloc byte[12];
            BinaryPrimitives.WriteUInt32LittleEndian(nonce, ConnectionId);
            BinaryPrimitives.WriteUInt64LittleEndian(nonce.Slice(4), sequence);

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

            // Increment SeqTx to keep crypto sequence in sync
            SeqTx++;
            BytesTransmitted += (ulong)ciphertextLength;
            return true;
        }
        catch (Exception ex)
        {
#if DEBUG
            ServerMonitor.Log($"[CRYPTO] Encrypt failed: {ex.Message}");
#endif
            ciphertextLength = 0;
            return false;
        }
    }

    public bool EncryptPayloadWithSequence(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad, ulong sequence, Span<byte> result, out int resultLength, out bool wasCompressed)
    {
        try
        {
            wasCompressed = false;
            resultLength = 0;

            // Use specific sequence for nonce generation
            Span<byte> nonce = stackalloc byte[12];
            BinaryPrimitives.WriteUInt32LittleEndian(nonce, ConnectionId);
            BinaryPrimitives.WriteUInt64LittleEndian(nonce.Slice(4), sequence);

            var cipher = new ChaCha20Poly1305();
            Span<byte> encrypted = stackalloc byte[plaintext.Length + 16];

            unsafe
            {
                fixed (byte* txKeyPtr = TxKey)
                {
                    var keyParam = new KeyParameter(new ReadOnlySpan<byte>(txKeyPtr, 32).ToArray());
                    var parameters = new AeadParameters(keyParam, 128, nonce.ToArray(), aad.ToArray());

                    cipher.Init(true, parameters);
                    int encryptedLength = cipher.ProcessBytes(plaintext.ToArray(), 0, plaintext.Length, encrypted.ToArray(), 0);
                    encryptedLength += cipher.DoFinal(encrypted.Slice(encryptedLength).ToArray(), 0);

                    // Increment SeqTx to keep crypto sequence in sync
                    SeqTx++;

                    if (encryptedLength > 512)
                    {
                        unsafe
                        {
                            fixed (byte* encryptedPtr = encrypted)
                            fixed (byte* resultPtr = result)
                            {
                                int compressedLength = LZ4.Compress(encryptedPtr, encryptedLength, resultPtr, result.Length);
                                if (compressedLength > 0 && compressedLength < encryptedLength)
                                {
                                    wasCompressed = true;
                                    resultLength = compressedLength;
                                    return true;
                                }
                            }
                        }
                    }

                    if (encryptedLength <= result.Length)
                    {
                        encrypted.Slice(0, encryptedLength).CopyTo(result);
                        resultLength = encryptedLength;
                        return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            wasCompressed = false;
            resultLength = 0;
            return false;
        }
    }

    public bool DecryptPayload(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> aad, ulong sequence, Span<byte> plaintext, out int plaintextLength)
    {
        try
        {
                                    // Only log for monitoring, not every packet
            if (sequence % 100 == 0) // Log every 100th packet for monitoring
                FileLogger.Log($"[DECRYPT] seq={sequence}, SeqRx={SeqRx}, HighestSeq={_highestSeqReceived}");

            // Log crypto data for comparison (only first packet)
            if (SeqRx == 0 && sequence == 0)
            {
                FileLogger.Log($"=== DECRYPTING FIRST PACKET ===");
                FileLogger.Log($"[SERVER] ConnectionId: {ConnectionId}");
                FileLogger.Log($"[SERVER] RxKey: {GetRxKeyHex()}");
            }

            if (!IsSequenceValid(sequence))
            {
#if DEBUG
                ServerMonitor.Log($"[CRYPTO] Decrypt - Replay attack detected! Sequence: {sequence}");
#endif
                plaintextLength = 0;
                return false;
            }

            Span<byte> nonce = stackalloc byte[12];
            BinaryPrimitives.WriteUInt32LittleEndian(nonce, ConnectionId);
            BinaryPrimitives.WriteUInt64LittleEndian(nonce.Slice(4), sequence);

                        // Log crypto data for comparison (only first packet)
            if (sequence == 0)
            {
                FileLogger.LogHex("[SERVER] Nonce", nonce);
                FileLogger.LogHex("[SERVER] AAD", aad);
                FileLogger.LogHex("[SERVER] Ciphertext", ciphertext);
                FileLogger.Log($"[SERVER] Ciphertext Length: {ciphertext.Length} bytes");
            }

            var cipher = new ChaCha20Poly1305();

            unsafe
            {
                fixed (byte* rxKeyPtr = RxKey)
                {
                    var keyParam = new KeyParameter(new ReadOnlySpan<byte>(rxKeyPtr, 32).ToArray());
                    var parameters = new AeadParameters(keyParam, 128, nonce.ToArray(), aad.ToArray());

                    cipher.Init(false, parameters);

                    // Create arrays for crypto operations
                    byte[] ciphertextArray = ciphertext.ToArray();
                    byte[] plaintextArray = new byte[plaintext.Length];

                    // Log detailed crypto operation for first packet
                    if (sequence == 0)
                    {
                        FileLogger.Log($"[SERVER] Starting decryption...");
                        FileLogger.Log($"[SERVER] Input ciphertext array length: {ciphertextArray.Length}");
                        FileLogger.Log($"[SERVER] Output plaintext array length: {plaintextArray.Length}");
                    }

                    plaintextLength = cipher.ProcessBytes(ciphertextArray, 0, ciphertextArray.Length, plaintextArray, 0);
                    int finalLength = cipher.DoFinal(plaintextArray, plaintextLength);
                    plaintextLength += finalLength;

                    // Log the results for first packet
                    if (sequence == 0)
                    {
                        FileLogger.Log($"[SERVER] ProcessBytes returned: {plaintextLength - finalLength} bytes");
                        FileLogger.Log($"[SERVER] DoFinal returned: {finalLength} bytes");
                        FileLogger.Log($"[SERVER] Total plaintext length: {plaintextLength} bytes");
                        FileLogger.LogHex("[SERVER] Raw decrypted data", new ReadOnlySpan<byte>(plaintextArray, 0, plaintextLength));
                    }

                    // Copy result back to span
                    new ReadOnlySpan<byte>(plaintextArray, 0, plaintextLength).CopyTo(plaintext);
                }
            }

            UpdateReplayWindow(sequence);

            // Log success for first packet
            if (sequence == 0)
            {
                FileLogger.Log($"[SERVER] ✅ DECRYPTION SUCCESS! Plaintext length: {plaintextLength} bytes");
            }

            return true;
        }
        catch (Exception ex)
        {
#if DEBUG
            ServerMonitor.Log($"[CRYPTO] Decrypt failed: {ex.Message}");
#endif
            // Log failure details
            if (sequence == 0)
            {
                FileLogger.Log($"[SERVER] ❌ DECRYPTION FAILED: {ex.Message}");
                FileLogger.Log($"[SERVER] Exception Type: {ex.GetType().Name}");
            }

            plaintextLength = 0;
            return false;
        }
    }

    public bool DecryptPayloadWithDecompression(ReadOnlySpan<byte> data, ReadOnlySpan<byte> aad, ulong sequence, bool isCompressed, Span<byte> plaintext, out int plaintextLength)
    {
        try
        {
            plaintextLength = 0;

            if (isCompressed)
            {
                Span<byte> decompressed = stackalloc byte[data.Length * 4];

                unsafe
                {
                    fixed (byte* dataPtr = data)
                    fixed (byte* decompressedPtr = decompressed)
                    {
                        int decompressedLength = LZ4.Decompress(dataPtr, data.Length, decompressedPtr, decompressed.Length);
                        if (decompressedLength <= 0)
                            return false;

                        return DecryptPayload(decompressed.Slice(0, decompressedLength), aad, sequence, plaintext, out plaintextLength);
                    }
                }
            }
            else
            {
                return DecryptPayload(data, aad, sequence, plaintext, out plaintextLength);
            }
        }
        catch
        {
            plaintextLength = 0;
            return false;
        }
    }

    private bool IsSequenceValid(ulong sequence)
    {
        if (sequence > _highestSeqReceived)
            return true;

        if (sequence + ReplayWindowSize <= _highestSeqReceived)
            return false;

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
            ulong shift = sequence - _highestSeqReceived;
            if (shift >= ReplayWindowSize)
            {
                _replayWindow = 1;
            }
            else
            {
                _replayWindow = (_replayWindow << (int)shift) | 1;
            }
            _highestSeqReceived = sequence;
        }
        else
        {
            ulong diff = _highestSeqReceived - sequence;
            if (diff < ReplayWindowSize)
            {
                ulong mask = 1UL << (int)diff;
                _replayWindow |= mask;
            }
        }
    }

    public bool ShouldRekey()
    {
        return BytesTransmitted >= RekeyBytesThreshold ||
               (DateTime.UtcNow - SessionStartTime) >= RekeyTimeThreshold;
    }

    public bool PerformRekey()
    {
        try
        {
            var rekeyContext = new List<byte>();
            rekeyContext.AddRange(Encoding.ASCII.GetBytes("rekey"));
            rekeyContext.AddRange(BitConverter.GetBytes(Math.Max(SeqTx, SeqRx)));

            Span<byte> okm = stackalloc byte[64];
            var hkdf = new HkdfBytesGenerator(new Sha256Digest());

            unsafe
            {
                fixed (byte* saltPtr = SessionSalt)
                {
                    hkdf.Init(new HkdfParameters(new ReadOnlySpan<byte>(saltPtr, 16).ToArray(), null, rekeyContext.ToArray()));
                    hkdf.GenerateBytes(okm);

                    fixed (byte* txKeyPtr = TxKey)
                    fixed (byte* rxKeyPtr = RxKey)
                    {
                        okm.Slice(0, 32).CopyTo(new Span<byte>(txKeyPtr, 32));
                        okm.Slice(32, 32).CopyTo(new Span<byte>(rxKeyPtr, 32));
                    }
                }
            }

            BytesTransmitted = 0;
            SessionStartTime = DateTime.UtcNow;

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

