#pragma once

#include "CoreMinimal.h"
#include "EntityDelta.generated.h"

UENUM(BlueprintType, meta = (Bitflags))
enum class EEntityDelta : uint8
{
    None      = 0 UMETA(DisplayName = "None"),
    Position  = 1 << 0 UMETA(DisplayName = "Position"),
    Rotation  = 1 << 1 UMETA(DisplayName = "Rotation"),
    AnimState = 1 << 2 UMETA(DisplayName = "AnimState"),
    Flags     = 1 << 3 UMETA(DisplayName = "Flags"),
    Velocity  = 1 << 4 UMETA(DisplayName = "Velocity"),
};

ENUM_CLASS_FLAGS(EEntityDelta)

USTRUCT(BlueprintType)
struct FDeltaUpdateData
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Index;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, meta = (Bitmask, BitmaskEnum = "EEntityDelta"))
    EEntityDelta EntitiesMask;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FVector Positon;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FRotator Rotator;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FVector Velocity;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 AnimationState;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Flags;
};

