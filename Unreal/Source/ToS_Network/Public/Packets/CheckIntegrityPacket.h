// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "CheckIntegrityPacket.generated.h"

USTRUCT(BlueprintType)
struct FCheckIntegrityPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Index;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Version;


    int32 GetSize() const { return 7; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        Index = static_cast<int32>(Buffer->ReadUInt16());
        Version = static_cast<int32>(Buffer->ReadUInt32());
    }
};
