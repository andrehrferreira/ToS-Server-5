#pragma once

#include "CoreMinimal.h"

enum class EClientPackets : uint16
{
    SyncEntity = 0,
    Pong = 1,
    EnterToWorld = 2,
};
