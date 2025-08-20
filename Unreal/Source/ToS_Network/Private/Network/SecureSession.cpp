#include "Network/SecureSession.h"
#include "HAL/UnrealMemory.h"

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

    // Perform X25519 key agreement
    uint8 SharedSecret[32];
    if (crypto_scalarmult_curve25519(SharedSecret, ClientPrivateKey.GetData(), ServerPublicKey.GetData()) != 0)
        return false;

        // Use HKDF-like derivation with libsodium KDF
    // Since libsodium doesn't have direct HKDF, we'll use crypto_kdf_derive_from_key
    uint8 MasterKey[32];
    crypto_generichash(MasterKey, 32, SharedSecret, 32, Salt.GetData(), 16);

    // Derive TX key (Client->Server)
    if (crypto_kdf_derive_from_key(TxKey, 32, 1, "ToS-UE5v", MasterKey) != 0)
        return false;

        // Derive RX key (Server->Client)
    if (crypto_kdf_derive_from_key(RxKey, 32, 2, "ToS-UE5v", MasterKey) != 0)
        return false;

    // Copy session salt
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
        return false;

    Ciphertext.SetNum(CiphertextLen);
    SeqTx++;
    return true;
}

bool FSecureSession::DecryptPayload(const TArray<uint8>& Ciphertext, const TArray<uint8>& AAD, uint64 Sequence, TArray<uint8>& Plaintext)
{
    // Check for replay attack
    if (!IsSequenceValid(Sequence))
        return false;

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
        return false;

    Plaintext.SetNum(PlaintextLen);
    UpdateReplayWindow(Sequence);
    return true;
}

bool FSecureSession::IsSequenceValid(uint64 Sequence) const
{
    // Accept if sequence is higher than any we've seen
    if (Sequence > HighestSeqReceived)
        return true;

    // Reject if too old (outside window)
    if (Sequence + ReplayWindowSize <= HighestSeqReceived)
        return false;

    // Check if we've already seen this sequence number
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
        // Shift window forward
        uint64 Shift = Sequence - HighestSeqReceived;
        if (Shift >= ReplayWindowSize)
        {
            ReplayWindow = 1; // Only the current bit set
        }
        else
        {
            ReplayWindow = (ReplayWindow << Shift) | 1;
        }
        HighestSeqReceived = Sequence;
    }
    else
    {
        // Mark this sequence as seen
        uint64 Diff = HighestSeqReceived - Sequence;
        if (Diff < ReplayWindowSize)
        {
            uint64 Mask = 1ULL << Diff;
            ReplayWindow |= Mask;
        }
    }
}
