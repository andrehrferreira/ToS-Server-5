#pragma once

#include "CoreMinimal.h"

enum class ClientPackets : uint16
{
    SyncEntity = 0,
    Pong = 1,
};
