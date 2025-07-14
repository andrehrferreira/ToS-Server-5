// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "PingPacket.generated.h"

USTRUCT(BlueprintType)
struct FPingPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 SentTimestamp;


    int32 GetSize() const { return 3; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        SentTimestamp = static_cast<int32>(Buffer->ReadUInt16());
    }
};
