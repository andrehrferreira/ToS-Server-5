#include "Entities/SyncEntity.h"
#include "Network/ENetSubsystem.h"
#include "Controllers/ToS_GameInstance.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Animation/AnimInstance.h"
#include "Utils/FileLogger.h"

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

    TargetLocation = GetActorLocation();
    TargetRotation = GetActorRotation();

	if (UWorld* World = GetWorld())
	{
		if (UTOSGameInstance* TosGameInstance = Cast<UTOSGameInstance>(World->GetGameInstance()))
		{
			NetSubsystem = TosGameInstance->GetSubsystem<UENetSubsystem>();
			UE_LOG(LogTemp, Warning, TEXT("🔧 SyncEntity: NetSubsystem found: %s"), NetSubsystem ? TEXT("YES") : TEXT("NO"));
			ClientFileLog(FString::Printf(TEXT("🔧 SyncEntity: NetSubsystem found: %s"), NetSubsystem ? TEXT("YES") : TEXT("NO")));
		}
		else
		{
			NetSubsystem = nullptr;
			UE_LOG(LogTemp, Error, TEXT("❌ SyncEntity: TOSGameInstance not found!"));
			ClientFileLog(TEXT("❌ SyncEntity: TOSGameInstance not found!"));
		}
	}
	else
	{
		NetSubsystem = nullptr;
		UE_LOG(LogTemp, Error, TEXT("❌ SyncEntity: World not found!"));
		ClientFileLog(TEXT("❌ SyncEntity: World not found!"));
	}
}

void ASyncEntity::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
    if (GetWorld())
		GetWorld()->GetTimerManager().ClearTimer(NetSyncTimerHandle);

    Super::EndPlay(EndPlayReason);
}

void ASyncEntity::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);

	if (!LocalControl) {
		const float InterpSpeed = 10.0f;

		FVector NewLocation = FMath::VInterpTo(GetActorLocation(), TargetLocation, DeltaTime, InterpSpeed);
		SetActorLocation(NewLocation);

		FRotator NewRotation = FMath::RInterpTo(GetActorRotation(), TargetRotation, DeltaTime, InterpSpeed);
		SetActorRotation(NewRotation);
	}
}

void ASyncEntity::UpdateAnimationFromNetwork(FVector Velocity, uint32 Animation, bool IsFalling)
{
	if (UCharacterMovementComponent* Movement = GetCharacterMovement())
	{
		Movement->Velocity = Velocity;
		Movement->RequestDirectMove(Velocity.GetSafeNormal() * Movement->GetMaxSpeed(), false);
		Movement->SetMovementMode(IsFalling ? MOVE_Falling : MOVE_Walking);
	}

	//UE_LOG(LogTemp, Warning, TEXT("UpdateAnimationFromNetwork: %s."), *Velocity.ToString());

    AnimationState = static_cast<int32>(Animation);
    SetSpeed(Velocity.Size());
}

void ASyncEntity::UpdateFromQuantizedNetwork(int16 QuantizedX, int16 QuantizedY, int16 QuantizedZ,
    int16 QuadrantX, int16 QuadrantY, float Yaw, FVector Velocity, uint32 Animation, bool IsFalling)
{
    // Convert quantized position back to world position
    const float Scale = 100.0f; // Match server scale
    const float QuadrantSize = 25600.0f * 4; // Section size * sections per component

    float WorldX = (QuadrantX * QuadrantSize) + (QuantizedX * Scale);
    float WorldY = (QuadrantY * QuadrantSize) + (QuantizedY * Scale);
    float WorldZ = QuantizedZ * Scale;

    FVector WorldPosition = FVector(WorldX, WorldY, WorldZ);
    FRotator WorldRotation = FRotator(0.0f, Yaw, 0.0f); // Only Yaw for optimization

    static int32 QuantizedUpdateCount = 0;
    QuantizedUpdateCount++;

    // Contador de atualizações para esta entidade específica
    static TMap<int32, int32> EntityUpdateCounts;
    int32& UpdateCount = EntityUpdateCounts.FindOrAdd(EntityId, 0);
    UpdateCount++;

    // Validação de posição para evitar saltos abruptos
    const FVector CurrentLocation = GetActorLocation();
    const float MaxAllowedDistance = 1000.0f; // 10 metros (1 unidade = 1cm)
    const float DistanceToNewPosition = FVector::Distance(CurrentLocation, WorldPosition);

    bool IsValidPosition = true;

    // Verificar se a posição é zero (posição inválida comum)
    if (FMath::IsNearlyZero(WorldPosition.X, 0.1f) &&
        FMath::IsNearlyZero(WorldPosition.Y, 0.1f) &&
        FMath::IsNearlyZero(WorldPosition.Z, 0.1f))
    {
        // Posição zero só é válida se for a primeira atualização e a entidade estiver na origem
        if (UpdateCount > 1 || !FMath::IsNearlyZero(CurrentLocation.X, 0.1f) || !FMath::IsNearlyZero(CurrentLocation.Y, 0.1f))
        {
            ClientFileLog(FString::Printf(TEXT("[ENTITY] ⚠️ Ignorando posição zero para Entity %d"), EntityId));
            IsValidPosition = false;
        }
    }

    // Se for uma das primeiras atualizações (até 3), aceitar qualquer posição para inicialização correta
    if (UpdateCount <= 3)
    {
        ClientFileLog(FString::Printf(TEXT("[ENTITY] 🔰 Aceitando posição inicial para Entity %d: %s"),
            EntityId, *WorldPosition.ToString()));
        IsValidPosition = true;
    }
    // Caso contrário, verificar se a posição está muito distante da atual
    else if (DistanceToNewPosition > MaxAllowedDistance)
    {
        // Verificar se é um teletransporte legítimo ou um erro
        // Se a velocidade for alta, pode ser um teletransporte legítimo
        if (Velocity.Size() < 1000.0f) // Velocidade menor que 10m/s
        {
            ClientFileLog(FString::Printf(TEXT("[ENTITY] ⚠️ Posição muito distante (%.2f unidades) para Entity %d - Interpolando gradualmente"),
                DistanceToNewPosition, EntityId));

            // Em vez de ignorar completamente, vamos interpolar gradualmente
            // Mover 20% da distância em direção à nova posição
            WorldPosition = CurrentLocation + (WorldPosition - CurrentLocation) * 0.2f;
        }
        else
        {
            ClientFileLog(FString::Printf(TEXT("[ENTITY] 🚀 Teletransporte detectado para Entity %d (distância: %.2f)"),
                EntityId, DistanceToNewPosition));
        }
    }

    if (IsValidPosition)
    {
        // Update target position and rotation for interpolation
        TargetLocation = WorldPosition;
        TargetRotation = WorldRotation;

        // Update animation
        UpdateAnimationFromNetwork(Velocity, Animation, IsFalling);
    }

    if (QuantizedUpdateCount <= 15)
    {
        ClientFileLog(FString::Printf(TEXT("=== UpdateFromQuantizedNetwork #%d (Entity Update #%d) ==="),
            QuantizedUpdateCount, UpdateCount));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] EntityId: %d"), EntityId));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Quantized: (%d, %d, %d)"), QuantizedX, QuantizedY, QuantizedZ));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Quadrant: (%d, %d)"), QuadrantX, QuadrantY));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Yaw: %f"), Yaw));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Velocity: %s"), *Velocity.ToString()));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Animation: %d, IsFalling: %s"), Animation, IsFalling ? TEXT("true") : TEXT("false")));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Calculated world position: %s"), *WorldPosition.ToString()));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Current location: %s"), *CurrentLocation.ToString()));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Distance to new position: %.2f units"), DistanceToNewPosition));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Position valid: %s"), IsValidPosition ? TEXT("YES") : TEXT("NO")));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] ✅ UpdateFromQuantizedNetwork completed for Entity %d"), EntityId));

        UE_LOG(LogTemp, Warning, TEXT("🎯 SyncEntity: UpdateFromQuantizedNetwork #%d (Entity Update #%d)"),
            QuantizedUpdateCount, UpdateCount);
        UE_LOG(LogTemp, Warning, TEXT("🎯 Quadrant: X=%d Y=%d"), QuadrantX, QuadrantY);
        UE_LOG(LogTemp, Warning, TEXT("🎯 Quantized: X=%d Y=%d Z=%d"), QuantizedX, QuantizedY, QuantizedZ);
        UE_LOG(LogTemp, Warning, TEXT("🎯 World Position: %s"), *WorldPosition.ToString());
        UE_LOG(LogTemp, Warning, TEXT("🎯 Distance: %.2f, Valid: %s"), DistanceToNewPosition, IsValidPosition ? TEXT("YES") : TEXT("NO"));
        UE_LOG(LogTemp, Warning, TEXT("🎯 Yaw: %f"), Yaw);
    }
}

void ASyncEntity::SetFlags(EEntityState Flags)
{
    EntityFlags = Flags;

    // Log para debug
    ClientFileLog(FString::Printf(TEXT("[ENTITY] Setting flags for Entity %d: %d"), EntityId, static_cast<int32>(Flags)));
}

void ASyncEntity::ClientFileLog(const FString& Message)
{
    // Usar a classe UE_LOG para logging
    UE_LOG(LogTemp, Log, TEXT("%s"), *Message);

    // Usar a função global ClientFileLog
    ::ClientFileLog(Message);
}
