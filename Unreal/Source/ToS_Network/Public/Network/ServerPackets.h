#pragma once

#include "CoreMinimal.h"

UENUM(BlueprintType)
enum class EServerPackets : uint8
{
    Benchmark = 0,
    CreateEntity = 1,
    UpdateEntity = 2,
    RemoveEntity = 3,
    DeltaSync = 4,
    SyncStateInt = 5,
};

template<> TOS_NETWORK_API UEnum* StaticEnum<EServerPackets>();
