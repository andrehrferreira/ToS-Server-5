// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "ConnectionDeniedPacket.generated.h"

USTRUCT(BlueprintType)
struct FConnectionDeniedPacket
{
    GENERATED_USTRUCT_BODY();


    int32 GetSize() const { return 1; }

};
