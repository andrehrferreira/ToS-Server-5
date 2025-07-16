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

    void UpdateAnimationFromNetwork(uint32 Speed, uint32 Animation);

    UFUNCTION(BlueprintImplementableEvent, Category = "Network")
    void SetSpeed(float Speed);

	int32 AnimationState = 0;

protected:
	virtual void BeginPlay() override;
	virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

	UPROPERTY()
	UENetSubsystem* NetSubsystem = nullptr;

	FTimerHandle NetSyncTimerHandle;
};
