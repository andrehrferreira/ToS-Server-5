// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "DeltaSyncPacket.generated.h"

USTRUCT(BlueprintType)
struct FDeltaSyncPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Index;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    uint8 EntitiesMask;


    int32 GetSize() const { return 8; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        Index = static_cast<int32>(Buffer->ReadUInt32());
        EntitiesMask = Buffer->ReadByte();
    }
};
