// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "CreateEntityPacket.generated.h"

USTRUCT(BlueprintType)
struct FCreateEntityPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 EntityId;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FVector Positon;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FRotator Rotator;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Flags;


    int32 GetSize() const { return 23; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        EntityId = static_cast<int32>(Buffer->Read<uint32>());
        Positon = Buffer->Read<FVector>();
        Rotator = Buffer->Read<FRotator>();
        Flags = static_cast<int32>(Buffer->Read<uint32>());
    }
};
