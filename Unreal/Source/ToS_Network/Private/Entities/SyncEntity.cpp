#include "Entities/SyncEntity.h"
#include "Network/ENetSubsystem.h"
#include "Controllers/ToS_GameInstance.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Animation/AnimInstance.h"

ASyncEntity::ASyncEntity()
{
	PrimaryActorTick.bCanEverTick = true;
}

void ASyncEntity::BeginPlay()
{
	Super::BeginPlay();

	if (UWorld* World = GetWorld())
	{
		if (UTOSGameInstance* TosGameInstance = Cast<UTOSGameInstance>(World->GetGameInstance()))		
			NetSubsystem = TosGameInstance->GetSubsystem<UENetSubsystem>();		
		else		
			NetSubsystem = nullptr;
	}
	else
	{
		NetSubsystem = nullptr;
	}
}

void ASyncEntity::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
        if (GetWorld())
                GetWorld()->GetTimerManager().ClearTimer(NetSyncTimerHandle);

        Super::EndPlay(EndPlayReason);
}

void ASyncEntity::UpdateAnimationFromNetwork(float Speed, uint32 AnimationState)
{
        if (UCharacterMovementComponent* MoveComp = GetCharacterMovement())
        {
                FVector Forward = GetActorForwardVector() * Speed;
                MoveComp->Velocity = Forward;
        }

        this->AnimationState = static_cast<int32>(AnimationState);
        SetSpeed(Speed);
}