#include "Entities/SyncEntity.h"
#include "Network/ENetSubsystem.h"
#include "Controllers/ToS_GameInstance.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Animation/AnimInstance.h"

ASyncEntity::ASyncEntity()
{
	PrimaryActorTick.bCanEverTick = true;

	bUseControllerRotationPitch = false;
	bUseControllerRotationYaw = false;
	bUseControllerRotationRoll = false;

	GetCharacterMovement()->bOrientRotationToMovement = true;
	GetCharacterMovement()->RotationRate = FRotator(0.0f, 500.0f, 0.0f);

	GetCharacterMovement()->JumpZVelocity = 700.f;
	GetCharacterMovement()->AirControl = 0.35f;
	GetCharacterMovement()->MaxWalkSpeed = 500.f;
	GetCharacterMovement()->MinAnalogWalkSpeed = 20.f;
	GetCharacterMovement()->BrakingDecelerationWalking = 2000.f;
	GetCharacterMovement()->BrakingDecelerationFalling = 1500.0f;
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

void ASyncEntity::UpdateAnimationFromNetwork(FVector Velocity, uint32 Animation)
{
	/*const float MaxSpeed = GetCharacterMovement()->MaxWalkSpeed;
	float Scale = FMath::Clamp(static_cast<float>(Speed) / MaxSpeed, 0.f, 1.f);

	const FRotator Rotation = GetActorRotation();
	const FRotator YawRotation(0, Rotation.Yaw, 0);

	const FVector ForwardDirection = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::X);
	const FVector RightDirection = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::Y);

	FVector2D FakeInput = FVector2D(0.7f, 0.7f) * Scale; 

	AddMovementInput(ForwardDirection, FakeInput.Y);
	AddMovementInput(RightDirection, FakeInput.X);

	SetSpeed(static_cast<float>(Speed));*/
	//AnimationState = static_cast<int32>(Animation);
}