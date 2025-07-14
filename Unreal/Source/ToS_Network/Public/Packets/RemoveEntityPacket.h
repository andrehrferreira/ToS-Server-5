// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "RemoveEntityPacket.generated.h"

USTRUCT(BlueprintType)
struct FRemoveEntityPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 EntityId;


    int32 GetSize() const { return 7; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        EntityId = static_cast<int32>(Buffer->ReadUInt32());
    }
};
