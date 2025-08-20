// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "ConnectionAcceptedPacket.generated.h"

USTRUCT(BlueprintType)
struct FConnectionAcceptedPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Id;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    TArray<uint8> ServerPublicKey;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    TArray<uint8> Salt;

    int32 GetSize() const { return 53; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        Id = static_cast<int32>(Buffer->Read<uint32>());
        ServerPublicKey.SetNumUninitialized(32);
        for (int32 i = 0; i < 32; ++i)
            ServerPublicKey[i] = Buffer->ReadByte();
        Salt.SetNumUninitialized(16);
        for (int32 i = 0; i < 16; ++i)
            Salt[i] = Buffer->ReadByte();
    }
};
