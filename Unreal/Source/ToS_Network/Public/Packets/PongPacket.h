// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ClientPackets.h"
#include "PongPacket.generated.h"

USTRUCT(BlueprintType)
struct FPongPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 SentTimestamp;


    int32 GetSize() const { return 3; }

    void Serialize(UFlatBuffer* Buffer)
    {
        Buffer->Write<uint8>(static_cast<uint8>(EPacketType::Pong));
        Buffer->Write<uint16>(static_cast<uint16>(SentTimestamp));
    }

};
