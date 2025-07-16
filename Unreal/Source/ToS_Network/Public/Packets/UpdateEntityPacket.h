// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "UpdateEntityPacket.generated.h"

USTRUCT(BlueprintType)
struct FUpdateEntityPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 EntityId;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FVector Positon;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FRotator Rotator;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Speed;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 AnimationState;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Flags;


    int32 GetSize() const { return 41; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        EntityId = static_cast<int32>(Buffer->Read<uint32>());
        Positon = Buffer->Read<FVector>();
        Rotator = Buffer->Read<FRotator>();
        Speed = static_cast<int32>(Buffer->Read<uint32>());
        AnimationState = static_cast<int32>(Buffer->Read<uint16>());
        Flags = static_cast<int32>(Buffer->Read<uint32>());
    }
};
