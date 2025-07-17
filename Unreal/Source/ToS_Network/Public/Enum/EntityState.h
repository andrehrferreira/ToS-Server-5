#pragma once

#include "CoreMinimal.h"
#include "EntityState.generated.h"

UENUM(BlueprintType, meta = (Bitflags))
enum class EEntityState : uint8
{
    None         = 0 UMETA(DisplayName = "None"),
    IsAlive      = 1 << 0 UMETA(DisplayName = "IsAlive"),
    IsInCombat   = 1 << 1 UMETA(DisplayName = "IsInCombat"),
    IsMoving     = 1 << 2 UMETA(DisplayName = "IsMoving"),
    IsCasting    = 1 << 3 UMETA(DisplayName = "IsCasting"),
    IsInvisible  = 1 << 4 UMETA(DisplayName = "IsInvisible"),
    IsStunned    = 1 << 5 UMETA(DisplayName = "IsStunned"),
    IsFalling    = 1 << 6 UMETA(DisplayName = "IsFalling")
};

ENUM_CLASS_FLAGS(EEntityState)

FORCEINLINE bool HasEntityState(EEntityState Current, EEntityState Flag)
{
    return (static_cast<uint8>(Current) & static_cast<uint8>(Flag)) != 0;
}

FORCEINLINE void AddEntityState(EEntityState& Current, EEntityState Flag)
{
    Current = static_cast<EEntityState>(static_cast<uint8>(Current) | static_cast<uint8>(Flag));
}

FORCEINLINE void RemoveEntityState(EEntityState& Current, EEntityState Flag)
{
    Current = static_cast<EEntityState>(static_cast<uint8>(Current) & ~static_cast<uint8>(Flag));
}