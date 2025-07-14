// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "SyncStateIntPacket.generated.h"

USTRUCT(BlueprintType)
struct FSyncStateIntPacket
{
    GENERATED_USTRUCT_BODY();


    int32 GetSize() const { return 3; }

};
