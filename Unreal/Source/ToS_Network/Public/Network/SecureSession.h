#pragma once

#include "CoreMinimal.h"
#include <sodium.h>

UENUM(BlueprintType)
enum class EPacketChannel : uint8
{
    Unreliable = 0,
    ReliableOrdered = 1,
    ReliableUnordered = 2
};

UENUM(BlueprintType, meta = (Bitflags, UseEnumValuesAsMaskValuesInEditor = "true"))
enum class EPacketHeaderFlags : uint8
{
    None = 0,
    Encrypted = 1 << 0,
    AEAD_ChaCha20Poly1305 = 1 << 1,
    Rekey = 1 << 2,
    Fragment = 1 << 3
};
ENUM_CLASS_FLAGS(EPacketHeaderFlags)

struct TOS_NETWORK_API FPacketHeader
{
    uint32 ConnectionId = 0;
    EPacketChannel Channel = EPacketChannel::Unreliable;
    EPacketHeaderFlags Flags = EPacketHeaderFlags::None;
    uint64 Sequence = 0;

    static constexpr int32 Size = 14;

    void Serialize(uint8* Buffer) const;
    static FPacketHeader Deserialize(const uint8* Buffer);
    TArray<uint8> GetAAD() const;
};

class TOS_NETWORK_API FSecureSession
{
private:
    uint8 TxKey[32];        // Client->Server key
    uint8 RxKey[32];        // Server->Client key
    uint8 SessionSalt[16];
    uint64 SeqTx = 0;
    uint64 SeqRx = 0;
    uint32 ConnectionId = 0;
    uint64 ReplayWindow = 0;
    uint64 HighestSeqReceived = 0;

    static constexpr int32 ReplayWindowSize = 64;

public:
    bool InitializeAsClient(const TArray<uint8>& ClientPrivateKey, const TArray<uint8>& ServerPublicKey,
                           const TArray<uint8>& Salt, uint32 InConnectionId);

    void GenerateNonce(uint64 Sequence, TArray<uint8>& Nonce) const;

    bool EncryptPayload(const TArray<uint8>& Plaintext, const TArray<uint8>& AAD, TArray<uint8>& Ciphertext);

    bool DecryptPayload(const TArray<uint8>& Ciphertext, const TArray<uint8>& AAD, uint64 Sequence, TArray<uint8>& Plaintext);

    uint32 GetConnectionId() const { return ConnectionId; }
    uint64 GetSeqTx() const { return SeqTx; }

private:
    bool IsSequenceValid(uint64 Sequence) const;
    void UpdateReplayWindow(uint64 Sequence);
};
