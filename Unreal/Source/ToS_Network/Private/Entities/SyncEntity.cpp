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
        // Verificar se a TargetLocation é válida (não é zero nem NaN)
        bool IsValidTarget = true;
        if (FMath::IsNearlyZero(TargetLocation.X, 0.1f) &&
            FMath::IsNearlyZero(TargetLocation.Y, 0.1f) &&
            FMath::IsNearlyZero(TargetLocation.Z, 0.1f))
        {
            // Se a entidade já foi posicionada em algum lugar não-zero, não permitir voltar para zero
            if (!FMath::IsNearlyZero(GetActorLocation().X, 0.1f) ||
                !FMath::IsNearlyZero(GetActorLocation().Y, 0.1f))
            {
                IsValidTarget = false;

                static int32 ZeroTargetCount = 0;
                if (++ZeroTargetCount <= 5) {
                    ClientFileLog(FString::Printf(TEXT("[TICK] ⚠️ Ignorando TargetLocation zero para Entity %d"), EntityId));
                }
            }
        }

        if (FMath::IsNaN(TargetLocation.X) || FMath::IsNaN(TargetLocation.Y) || FMath::IsNaN(TargetLocation.Z))
        {
            IsValidTarget = false;

            static int32 NaNTargetCount = 0;
            if (++NaNTargetCount <= 5) {
                ClientFileLog(FString::Printf(TEXT("[TICK] ⚠️ Ignorando TargetLocation NaN para Entity %d"), EntityId));
            }
        }

        // Verificar se a distância para o alvo não é muito grande
        float DistanceToTarget = FVector::Distance(GetActorLocation(), TargetLocation);
        const float MaxSingleTickDistance = 2000.0f; // 20 metros

        if (DistanceToTarget > MaxSingleTickDistance)
        {
            static int32 LargeDistanceCount = 0;
            if (++LargeDistanceCount <= 5) {
                ClientFileLog(FString::Printf(TEXT("[TICK] ⚠️ Distância muito grande (%.2f) para Entity %d - Limitando movimento"),
                    DistanceToTarget, EntityId));
            }

            // Limitar o movimento máximo por tick
            FVector Direction = (TargetLocation - GetActorLocation()).GetSafeNormal();
            TargetLocation = GetActorLocation() + Direction * MaxSingleTickDistance * 0.5f;
        }

        if (IsValidTarget)
        {
            // Usar uma velocidade de interpolação adaptativa baseada na distância
            // Mais longe = mais rápido, mais perto = mais suave
            float BaseInterpSpeed = 10.0f;
            float AdaptiveInterpSpeed = FMath::Clamp(BaseInterpSpeed * (DistanceToTarget / 100.0f),
                                                    BaseInterpSpeed, BaseInterpSpeed * 3.0f);

            FVector NewLocation = FMath::VInterpTo(GetActorLocation(), TargetLocation, DeltaTime, AdaptiveInterpSpeed);
            SetActorLocation(NewLocation);

            FRotator NewRotation = FMath::RInterpTo(GetActorRotation(), TargetRotation, DeltaTime, BaseInterpSpeed);
            SetActorRotation(NewRotation);

            // Debug para as primeiras atualizações
            static int32 TickCount = 0;
            if (++TickCount <= 10) {
                ClientFileLog(FString::Printf(TEXT("[TICK] ✅ Entity %d movendo para %s (distância: %.2f, velocidade: %.2f)"),
                    EntityId, *TargetLocation.ToString(), DistanceToTarget, AdaptiveInterpSpeed));
            }
        }
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
    // Armazenar os valores originais para logging
    const int16 OriginalQuantizedX = QuantizedX;
    const int16 OriginalQuantizedY = QuantizedY;
    const int16 OriginalQuantizedZ = QuantizedZ;
    const int16 OriginalQuadrantX = QuadrantX;
    const int16 OriginalQuadrantY = QuadrantY;

    // Validar quadrantes - valores muito grandes ou pequenos são provavelmente erros
    const int16 MaxQuadrantValue = 100; // Limite razoável para quadrantes
    bool QuadrantAdjusted = false;

    if (QuadrantX < -MaxQuadrantValue || QuadrantX > MaxQuadrantValue)
    {
        QuadrantX = 0;
        QuadrantAdjusted = true;
    }

    if (QuadrantY < -MaxQuadrantValue || QuadrantY > MaxQuadrantValue)
    {
        QuadrantY = 0;
        QuadrantAdjusted = true;
    }

    if (QuadrantAdjusted)
    {
        ClientFileLog(FString::Printf(TEXT("[ENTITY] ⚠️ Quadrante inválido (%d, %d) para Entity %d - Ajustado para (%d, %d)"),
            OriginalQuadrantX, OriginalQuadrantY, EntityId, QuadrantX, QuadrantY));
    }

    // Convert quantized position back to world position
    const float Scale = 100.0f; // Match server scale
    const float QuadrantSize = 25600.0f * 4; // Section size * sections per component

    float WorldX = (QuadrantX * QuadrantSize) + (QuantizedX * Scale);
    float WorldY = (QuadrantY * QuadrantSize) + (QuantizedY * Scale);
    float WorldZ = QuantizedZ * Scale;

    FVector WorldPosition = FVector(WorldX, WorldY, WorldZ);
    FRotator WorldRotation = FRotator(0.0f, Yaw, 0.0f); // Only Yaw for optimization

    // Verificar valores NaN
    if (FMath::IsNaN(WorldPosition.X) || FMath::IsNaN(WorldPosition.Y) || FMath::IsNaN(WorldPosition.Z))
    {
        ClientFileLog(FString::Printf(TEXT("[ENTITY] ⚠️ Posição NaN detectada para Entity %d - Usando posição atual"), EntityId));
        WorldPosition = GetActorLocation();
    }

    static int32 QuantizedUpdateCount = 0;
    QuantizedUpdateCount++;

    // Contador de atualizações para esta entidade específica
    static TMap<int32, int32> EntityUpdateCounts;
    int32& UpdateCount = EntityUpdateCounts.FindOrAdd(EntityId, 0);
    UpdateCount++;

    // Rastrear última posição válida para cada entidade
    static TMap<int32, FVector> LastValidPositions;
    FVector& LastValidPosition = LastValidPositions.FindOrAdd(EntityId, GetActorLocation());

    // Validação de posição para evitar saltos abruptos
    const FVector CurrentLocation = GetActorLocation();
    const float MaxAllowedDistance = 1000.0f; // 10 metros (1 unidade = 1cm)
    const float DistanceToNewPosition = FVector::Distance(CurrentLocation, WorldPosition);
    const float DistanceFromLastValid = FVector::Distance(LastValidPosition, WorldPosition);

    bool IsValidPosition = true;
    bool IsZeroPosition = FMath::IsNearlyZero(WorldPosition.X, 0.1f) &&
                          FMath::IsNearlyZero(WorldPosition.Y, 0.1f) &&
                          FMath::IsNearlyZero(WorldPosition.Z, 0.1f);

    // Verificar se a posição é zero (posição inválida comum)
    if (IsZeroPosition)
    {
        // Posição zero só é válida se for a primeira atualização e a entidade estiver na origem
        if (UpdateCount > 1 || !FMath::IsNearlyZero(CurrentLocation.X, 0.1f) || !FMath::IsNearlyZero(CurrentLocation.Y, 0.1f))
        {
            ClientFileLog(FString::Printf(TEXT("[ENTITY] ⚠️ Ignorando posição zero para Entity %d"), EntityId));
            IsValidPosition = false;

            // Usar a última posição válida conhecida
            WorldPosition = LastValidPosition;
        }
    }

    // Se for uma das primeiras atualizações (até 3), aceitar qualquer posição para inicialização correta
    // desde que não seja zero (a menos que a entidade deva começar em zero)
    if (UpdateCount <= 3 && (!IsZeroPosition || UpdateCount == 1))
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

    // Verificar se a posição está muito longe da última posição válida conhecida
    // Isso ajuda a detectar saltos abruptos que podem ocorrer após várias atualizações inválidas
    if (DistanceFromLastValid > MaxAllowedDistance * 3.0f && UpdateCount > 5)
    {
        ClientFileLog(FString::Printf(TEXT("[ENTITY] ⚠️ Posição muito distante da última válida (%.2f unidades) para Entity %d - Interpolando"),
            DistanceFromLastValid, EntityId));

        // Interpolar em direção à nova posição, mas com um passo maior
        WorldPosition = LastValidPosition + (WorldPosition - LastValidPosition) * 0.3f;
    }

    // Se a posição for válida, atualizar a última posição válida conhecida
    if (IsValidPosition && !IsZeroPosition)
    {
        LastValidPosition = WorldPosition;
    }

    // Sempre atualizar a posição alvo, mesmo que seja uma interpolação
    // Isso garante que a entidade sempre se mova em direção à posição correta
    TargetLocation = WorldPosition;
    TargetRotation = WorldRotation;

    // Update animation
    UpdateAnimationFromNetwork(Velocity, Animation, IsFalling);

    if (QuantizedUpdateCount <= 15 || UpdateCount <= 5)
    {
        ClientFileLog(FString::Printf(TEXT("=== UpdateFromQuantizedNetwork #%d (Entity Update #%d) ==="),
            QuantizedUpdateCount, UpdateCount));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] EntityId: %d"), EntityId));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Quantized Original: (%d, %d, %d)"),
            OriginalQuantizedX, OriginalQuantizedY, OriginalQuantizedZ));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Quadrant Original: (%d, %d)"),
            OriginalQuadrantX, OriginalQuadrantY));

        if (QuadrantAdjusted)
        {
            ClientFileLog(FString::Printf(TEXT("[ENTITY] Quadrant Adjusted: (%d, %d)"), QuadrantX, QuadrantY));
        }

        ClientFileLog(FString::Printf(TEXT("[ENTITY] Yaw: %f"), Yaw));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Velocity: %s"), *Velocity.ToString()));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Animation: %d, IsFalling: %s"), Animation, IsFalling ? TEXT("true") : TEXT("false")));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Calculated world position: %s"), *WorldPosition.ToString()));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Current location: %s"), *CurrentLocation.ToString()));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Last valid position: %s"), *LastValidPosition.ToString()));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Distance to new position: %.2f units"), DistanceToNewPosition));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Distance from last valid: %.2f units"), DistanceFromLastValid));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] Position valid: %s"), IsValidPosition ? TEXT("YES") : TEXT("NO")));
        ClientFileLog(FString::Printf(TEXT("[ENTITY] ✅ UpdateFromQuantizedNetwork completed for Entity %d"), EntityId));

        UE_LOG(LogTemp, Warning, TEXT("🎯 SyncEntity: UpdateFromQuantizedNetwork #%d (Entity Update #%d)"),
            QuantizedUpdateCount, UpdateCount);
        UE_LOG(LogTemp, Warning, TEXT("🎯 Quadrant: X=%d Y=%d %s"), QuadrantX, QuadrantY,
            QuadrantAdjusted ? TEXT("(Adjusted)") : TEXT(""));
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
