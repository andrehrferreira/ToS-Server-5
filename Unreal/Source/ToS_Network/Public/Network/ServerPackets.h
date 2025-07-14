#pragma once

#include "CoreMinimal.h"

enum class EServerPackets : uint16
{
    Benchmark = 0,
    CreateEntity = 1,
    UpdateEntity = 2,
    RemoveEntity = 3,
    DeltaSync = 4,
    SyncStateInt = 5,
};
