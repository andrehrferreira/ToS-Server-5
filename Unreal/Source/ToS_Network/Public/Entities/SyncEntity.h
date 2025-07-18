#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "Network/ENetSubsystem.h"
#include "TimerManager.h"
#include "SyncEntity.generated.h"

UCLASS()
class TOS_NETWORK_API ASyncEntity : public ACharacter
{
	GENERATED_BODY()

public:
    ASyncEntity();

    UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = Entity)
    int32 EntityId;

    UPROPERTY(BlueprintReadWrite, Category = "Network")
    FVector TargetLocation;

    UPROPERTY(BlueprintReadWrite, Category = "Network")
    FRotator TargetRotation;

    void UpdateAnimationFromNetwork(FVector Velocity, uint32 Animation, bool IsFalling);

    UFUNCTION(BlueprintImplementableEvent, Category = "Network")
    void SetSpeed(float Speed);

	int32 AnimationState = 0;

	bool LocalControl = false;

protected:
    virtual void Tick(float DeltaTime) override;
    virtual void BeginPlay() override;
	virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

	UPROPERTY()
	UENetSubsystem* NetSubsystem = nullptr;

	FTimerHandle NetSyncTimerHandle;
};
