#include "Network/SecureSession.h"
#include "Utils/LZ4.h"
#include "Utils/FileLogger.h"
#include "HAL/UnrealMemory.h"

static FString BytesToHexString(const TArray<uint8>& Bytes)
{
    FString Result;
    Result.Reserve(Bytes.Num() * 2);

    for (uint8 Byte : Bytes)
    {
        Result += FString::Printf(TEXT("%02X"), Byte);
    }

    return Result;
}

void FPacketHeader::Serialize(uint8* Buffer) const
{
    FMemory::Memcpy(Buffer, &ConnectionId, 4);
    Buffer[4] = static_cast<uint8>(Channel);
    Buffer[5] = static_cast<uint8>(Flags);
    FMemory::Memcpy(Buffer + 6, &Sequence, 8);
}

FPacketHeader FPacketHeader::Deserialize(const uint8* Buffer)
{
    FPacketHeader Header;
    FMemory::Memcpy(&Header.ConnectionId, Buffer, 4);
    Header.Channel = static_cast<EPacketChannel>(Buffer[4]);
    Header.Flags = static_cast<EPacketHeaderFlags>(Buffer[5]);
    FMemory::Memcpy(&Header.Sequence, Buffer + 6, 8);
    return Header;
}

TArray<uint8> FPacketHeader::GetAAD() const
{
    TArray<uint8> AAD;
    AAD.SetNumUninitialized(Size);
    Serialize(AAD.GetData());
    return AAD;
}

bool FSecureSession::InitializeAsClient(const TArray<uint8>& ClientPrivateKey, const TArray<uint8>& ServerPublicKey,
                                       const TArray<uint8>& Salt, uint32 InConnectionId)
{
    if (ClientPrivateKey.Num() != 32 || ServerPublicKey.Num() != 32 || Salt.Num() != 16)
        return false;

    uint8 SharedSecret[32];
    if (crypto_scalarmult_curve25519(SharedSecret, ClientPrivateKey.GetData(), ServerPublicKey.GetData()) != 0)
        return false;

    uint8 PRK[32];
    crypto_auth_hmacsha256_state state;
    crypto_auth_hmacsha256_init(&state, Salt.GetData(), 16);
    crypto_auth_hmacsha256_update(&state, SharedSecret, 32);
    crypto_auth_hmacsha256_final(&state, PRK);

    const char* Info = "ToS-UE5 v1";
    uint8 OKM[64];
    uint8 T[32];

    crypto_auth_hmacsha256_init(&state, PRK, 32);
    crypto_auth_hmacsha256_update(&state, (const uint8*)Info, strlen(Info));
    crypto_auth_hmacsha256_update(&state, (const uint8*)"\x01", 1);
    crypto_auth_hmacsha256_final(&state, T);
    FMemory::Memcpy(OKM, T, 32);

    crypto_auth_hmacsha256_init(&state, PRK, 32);
    crypto_auth_hmacsha256_update(&state, T, 32);
    crypto_auth_hmacsha256_update(&state, (const uint8*)Info, strlen(Info));
    crypto_auth_hmacsha256_update(&state, (const uint8*)"\x02", 1);
    crypto_auth_hmacsha256_final(&state, T);
    FMemory::Memcpy(OKM + 32, T, 32);

    FMemory::Memcpy(TxKey, OKM + 32, 32);
    FMemory::Memcpy(RxKey, OKM, 32);

    FMemory::Memcpy(SessionSalt, Salt.GetData(), 16);

    ConnectionId = InConnectionId;
    SeqTx = 0;
    SeqRx = 0;
    ReplayWindow = 0;
    HighestSeqReceived = 0;

    return true;
}

void FSecureSession::GenerateNonce(uint64 Sequence, TArray<uint8>& Nonce) const
{
    Nonce.SetNumUninitialized(12);
    FMemory::Memcpy(Nonce.GetData(), &ConnectionId, 4);
    FMemory::Memcpy(Nonce.GetData() + 4, &Sequence, 8);
}

bool FSecureSession::EncryptPayload(const TArray<uint8>& Plaintext, const TArray<uint8>& AAD, TArray<uint8>& Ciphertext)
{
    TArray<uint8> Nonce;
    GenerateNonce(SeqTx, Nonce);

    // Log crypto data to file (only first packet)
    if (SeqTx == 0)
    {
        ClientFileLogHex(TEXT("[CLIENT] Nonce"), Nonce);
    }



    Ciphertext.SetNumUninitialized(Plaintext.Num() + crypto_aead_chacha20poly1305_ietf_ABYTES);

    unsigned long long CiphertextLen;
    int Result = crypto_aead_chacha20poly1305_ietf_encrypt(
        Ciphertext.GetData(), &CiphertextLen,
        Plaintext.GetData(), Plaintext.Num(),
        AAD.GetData(), AAD.Num(),
        nullptr,
        Nonce.GetData(), TxKey
    );

    if (Result != 0)
    {
        UE_LOG(LogTemp, Error, TEXT("[CRYPTO] Encrypt failed with result: %d"), Result);
        return false;
    }

    Ciphertext.SetNum(CiphertextLen);

    SeqTx++;

    // Log TxKey to file (only once)
    if (SeqTx == 1)
    {
        TArray<uint8> TxKeyArray;
        TxKeyArray.SetNumUninitialized(32);
        FMemory::Memcpy(TxKeyArray.GetData(), TxKey, 32);
        ClientFileLogHex(TEXT("[CLIENT] TxKey"), TxKeyArray);
    }

    return true;
}

bool FSecureSession::EncryptPayloadWithCompression(const TArray<uint8>& Plaintext, const TArray<uint8>& AAD, TArray<uint8>& Result, bool& bWasCompressed)
{
    bWasCompressed = false;

    TArray<uint8> Encrypted;

    if (!EncryptPayload(Plaintext, AAD, Encrypted))
        return false;

    if (Encrypted.Num() > 512)
    {
        TArray<uint8> Compressed;
        Compressed.SetNumUninitialized(Encrypted.Num() + 1024);

        int32 CompressedLength = FLZ4::Compress(Encrypted.GetData(), Encrypted.Num(), Compressed.GetData(), Compressed.Num());

        if (CompressedLength > 0 && CompressedLength < Encrypted.Num())
        {
            bWasCompressed = true;
            Result.SetNumUninitialized(CompressedLength);
            FMemory::Memcpy(Result.GetData(), Compressed.GetData(), CompressedLength);
            return true;
        }
    }

    Result = MoveTemp(Encrypted);
    return true;
}

bool FSecureSession::EncryptPayloadWithSequence(const TArray<uint8>& Plaintext, const TArray<uint8>& AAD, uint64 Sequence, TArray<uint8>& Result, bool& bWasCompressed)
{
    bWasCompressed = false;

    // Use specific sequence instead of internal SeqTx
    TArray<uint8> Nonce;
    GenerateNonce(Sequence, Nonce);

    TArray<uint8> Encrypted;
    Encrypted.SetNumUninitialized(Plaintext.Num() + crypto_aead_chacha20poly1305_ietf_ABYTES);

    unsigned long long CiphertextLen;
    int EncryptResult = crypto_aead_chacha20poly1305_ietf_encrypt(
        Encrypted.GetData(), &CiphertextLen,
        Plaintext.GetData(), Plaintext.Num(),
        AAD.GetData(), AAD.Num(),
        nullptr,
        Nonce.GetData(), TxKey
    );

    if (EncryptResult != 0)
    {
        UE_LOG(LogTemp, Error, TEXT("[CRYPTO] Encrypt failed with result: %d"), EncryptResult);
        return false;
    }

    Encrypted.SetNum(CiphertextLen);

    // Increment SeqTx to keep crypto sequence in sync
    SeqTx++;

    if (Encrypted.Num() > 512)
    {
        TArray<uint8> Compressed;
        Compressed.SetNumUninitialized(Encrypted.Num() + 1024);

        int32 CompressedLength = FLZ4::Compress(Encrypted.GetData(), Encrypted.Num(), Compressed.GetData(), Compressed.Num());

        if (CompressedLength > 0 && CompressedLength < Encrypted.Num())
        {
            bWasCompressed = true;
            Result.SetNumUninitialized(CompressedLength);
            FMemory::Memcpy(Result.GetData(), Compressed.GetData(), CompressedLength);
            return true;
        }
    }

    Result = MoveTemp(Encrypted);
    return true;
}

bool FSecureSession::EncryptPayloadWithSpecificSequence(const TArray<uint8>& Plaintext, const TArray<uint8>& AAD, uint64 Sequence, TArray<uint8>& Ciphertext)
{
    // Use specific sequence instead of internal SeqTx
    TArray<uint8> Nonce;
    GenerateNonce(Sequence, Nonce);

    Ciphertext.SetNumUninitialized(Plaintext.Num() + crypto_aead_chacha20poly1305_ietf_ABYTES);

    unsigned long long CiphertextLen;
    int Result = crypto_aead_chacha20poly1305_ietf_encrypt(
        Ciphertext.GetData(), &CiphertextLen,
        Plaintext.GetData(), Plaintext.Num(),
        AAD.GetData(), AAD.Num(),
        nullptr,
        Nonce.GetData(), TxKey
    );

    if (Result != 0)
    {
        UE_LOG(LogTemp, Error, TEXT("[CRYPTO] Encrypt failed with result: %d"), Result);
        return false;
    }

    Ciphertext.SetNum(CiphertextLen);

    // Increment SeqTx to keep crypto sequence in sync
    SeqTx++;
    return true;
}

bool FSecureSession::DecryptPayload(const TArray<uint8>& Ciphertext, const TArray<uint8>& AAD, uint64 Sequence, TArray<uint8>& Plaintext)
{
    if (!IsSequenceValid(Sequence))
    {
        UE_LOG(LogTemp, Warning, TEXT("[CRYPTO] Decrypt - Replay attack detected! Sequence: %llu"), Sequence);
        return false;
    }

    TArray<uint8> Nonce;
    Nonce.SetNumUninitialized(12);
    FMemory::Memcpy(Nonce.GetData(), &ConnectionId, 4);
    FMemory::Memcpy(Nonce.GetData() + 4, &Sequence, 8);



    Plaintext.SetNumUninitialized(Ciphertext.Num() - crypto_aead_chacha20poly1305_ietf_ABYTES);

    unsigned long long PlaintextLen;
    int Result = crypto_aead_chacha20poly1305_ietf_decrypt(
        Plaintext.GetData(), &PlaintextLen,
        nullptr,
        Ciphertext.GetData(), Ciphertext.Num(),
        AAD.GetData(), AAD.Num(),
        Nonce.GetData(), RxKey
    );

    if (Result != 0)
    {
        UE_LOG(LogTemp, Error, TEXT("[CRYPTO] Decrypt failed with result: %d"), Result);
        return false;
    }

    Plaintext.SetNum(PlaintextLen);

    UpdateReplayWindow(Sequence);
    return true;
}

bool FSecureSession::DecryptPayloadWithDecompression(const TArray<uint8>& Data, const TArray<uint8>& AAD, uint64 Sequence, bool bIsCompressed, TArray<uint8>& Plaintext)
{
    if (bIsCompressed)
    {
        TArray<uint8> Decompressed;
        Decompressed.SetNumUninitialized(Data.Num() * 4);

        int32 DecompressedLength = FLZ4::Decompress(Data.GetData(), Data.Num(), Decompressed.GetData(), Decompressed.Num());
        if (DecompressedLength <= 0)
            return false;

        Decompressed.SetNum(DecompressedLength);
        return DecryptPayload(Decompressed, AAD, Sequence, Plaintext);
    }
    else
    {
        return DecryptPayload(Data, AAD, Sequence, Plaintext);
    }
}

bool FSecureSession::IsSequenceValid(uint64 Sequence) const
{
    if (Sequence > HighestSeqReceived)
        return true;

    if (Sequence + ReplayWindowSize <= HighestSeqReceived)
        return false;

    uint64 Diff = HighestSeqReceived - Sequence;

    if (Diff < ReplayWindowSize)
    {
        uint64 Mask = 1ULL << Diff;
        return (ReplayWindow & Mask) == 0;
    }

    return false;
}

void FSecureSession::UpdateReplayWindow(uint64 Sequence)
{
    if (Sequence > HighestSeqReceived)
    {
        uint64 Shift = Sequence - HighestSeqReceived;

        if (Shift >= ReplayWindowSize)
        {
            ReplayWindow = 1;
        }
        else
        {
            ReplayWindow = (ReplayWindow << Shift) | 1;
        }

        HighestSeqReceived = Sequence;
    }
    else
    {
        uint64 Diff = HighestSeqReceived - Sequence;

        if (Diff < ReplayWindowSize)
        {
            uint64 Mask = 1ULL << Diff;
            ReplayWindow |= Mask;
        }
    }
}
