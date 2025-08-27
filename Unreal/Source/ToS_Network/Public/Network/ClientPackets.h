#pragma once

#include "CoreMinimal.h"

UENUM(BlueprintType)
enum class EClientPackets : uint8
{
    SyncEntity = 0,
    SyncEntityQuantized = 1,
    EnterToWorld = 2,
    RekeyResponse = 3,
};

template<> TOS_NETWORK_API UEnum* StaticEnum<EClientPackets>();
