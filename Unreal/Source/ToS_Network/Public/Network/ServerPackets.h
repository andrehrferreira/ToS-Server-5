#pragma once

#include "CoreMinimal.h"

enum class EServerPackets : uint16
{
    Benchmark = 0,
    CreateEntity = 1,
    UpdateEntity = 2,
    RemoveEntity = 3,
    Ping = 4,
    ConnectionAccepted = 5,
    ConnectionDenied = 6,
    Disconnect = 7,
    CheckIntegrity = 8,
    Ack = 9,
    DeltaSync = 10,
    SyncStateInt = 11,
};
