// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "ReliableHandshakePacket.generated.h"

USTRUCT(BlueprintType)
struct FReliableHandshakePacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FString Message;


    int32 GetSize() const { return 3601; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        Message = Buffer->ReadString();
    }
};
