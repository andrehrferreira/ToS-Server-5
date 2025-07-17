#pragma once

#include "CoreMinimal.h"

UENUM(BlueprintType)
enum class EClientPackets : uint8
{
    SyncEntity = 0,
    Pong = 1,
    EnterToWorld = 2,
};

template<> TOS_NETWORK_API UEnum* StaticEnum<EClientPackets>();
