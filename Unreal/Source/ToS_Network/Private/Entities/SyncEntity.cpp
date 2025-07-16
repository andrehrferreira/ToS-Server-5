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

void ASyncEntity::UpdateAnimationFromNetwork(uint32 Speed, uint32 Animation)
{
    if (UCharacterMovementComponent* MoveComp = GetCharacterMovement())
    {
        FVector Forward = GetActorForwardVector() * Speed;
        MoveComp->Velocity = Forward;
    }

    this->AnimationState = static_cast<int32>(Animation);
    SetSpeed(static_cast<float>(Speed));
}