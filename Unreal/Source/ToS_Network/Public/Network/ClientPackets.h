#pragma once

#include "CoreMinimal.h"

UENUM(BlueprintType)
enum class EClientPackets : uint8
{
    SyncEntity = 0,
    EnterToWorld = 1,
};

template<> TOS_NETWORK_API UEnum* StaticEnum<EClientPackets>();
