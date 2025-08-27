// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "UpdateEntityQuantizedPacket.generated.h"

USTRUCT(BlueprintType)
struct FUpdateEntityQuantizedPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 EntityId;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 QuantizedX;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 QuantizedY;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 QuantizedZ;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 QuadrantX;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 QuadrantY;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    float Yaw;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FVector Velocity;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 AnimationState;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Flags;


    int32 GetSize() const { return 33; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        EntityId = static_cast<int32>(Buffer->Read<uint32>());
        QuantizedX = static_cast<int32>(Buffer->Read<int16>());
        QuantizedY = static_cast<int32>(Buffer->Read<int16>());
        QuantizedZ = static_cast<int32>(Buffer->Read<int16>());
        QuadrantX = static_cast<int32>(Buffer->Read<int16>());
        QuadrantY = static_cast<int32>(Buffer->Read<int16>());
        Yaw = Buffer->Read<float>();
        Velocity = Buffer->Read<FVector>();
        AnimationState = static_cast<int32>(Buffer->Read<uint16>());
        Flags = static_cast<int32>(Buffer->Read<uint32>());
    }
};
