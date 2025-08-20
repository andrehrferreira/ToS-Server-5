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
        Buffer->ReadBytes(ServerPublicKey.GetData(), 32);
        Salt.SetNumUninitialized(16);
        Buffer->ReadBytes(Salt.GetData(), 16);
    }
};
