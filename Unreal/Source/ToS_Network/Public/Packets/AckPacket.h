// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "AckPacket.generated.h"

USTRUCT(BlueprintType)
struct FAckPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Sequence;


    int32 GetSize() const { return 3; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        Sequence = static_cast<int32>(Buffer->Read<int16>());
    }
};
