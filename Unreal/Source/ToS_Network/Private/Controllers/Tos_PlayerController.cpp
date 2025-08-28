#include "Controllers/ToS_PlayerController.h"
#include "Controllers/ToS_GameInstance.h"
#include "Enum/EntityState.h"
#include "Enum/EntityDelta.h"
#include "Engine/World.h"
#include "Utils/FileLogger.h"

void ATOSPlayerController::BeginPlay()
{
    Super::BeginPlay();

    if (UTOSGameInstance* GI = GetGameInstance<UTOSGameInstance>())
    {
        GI->OnPlayerControllerReady(this);
		EntityClass = GI->EntityClass;
        PlayerClass = GI->PlayerClass;
    }
    else
    {
        EntityClass = nullptr;
		PlayerClass = nullptr;
    }

    bIsReadyToSync = true;
}

ASyncEntity* ATOSPlayerController::GetEntityById(int32 Id)
{
    if (ASyncEntity** Found = SpawnedEntities.Find(Id))
    {
        return *Found;
    }

    return nullptr;
}

void ATOSPlayerController::HandleCreateEntity(int32 EntityId, FVector Position, FRotator Rotator, int32 Flags)
{
    static int32 CreateEntityCount = 0;
    CreateEntityCount++;

    if (!bIsReadyToSync)
    {
        ClientFileLog(FString::Printf(TEXT("[CREATE ENTITY] #%d ❌ Not ready to sync for EntityId: %d"),
            CreateEntityCount, EntityId));
        return;
    }

    // Verificar se a entidade já existe para evitar duplicação
    if (SpawnedEntities.Contains(EntityId))
    {
        ClientFileLog(FString::Printf(TEXT("[CREATE ENTITY] #%d ⚠️ Entity %d already exists - skipping creation"),
            CreateEntityCount, EntityId));
        return;
    }

    UWorld* World = GetWorld();
    if (!World || !EntityClass)
    {
        ClientFileLog(FString::Printf(TEXT("[CREATE ENTITY] #%d ❌ Invalid World or EntityClass for EntityId: %d"),
            CreateEntityCount, EntityId));
        return;
    }

    // Rastrear posições válidas para entidades
    static TMap<int32, FVector> LastValidPositions;

    // Validar posição para evitar valores inválidos
    bool IsValidPosition = true;

    // Verificar NaN
    if (FMath::IsNaN(Position.X) || FMath::IsNaN(Position.Y) || FMath::IsNaN(Position.Z))
    {
        ClientFileLog(FString::Printf(TEXT("[CREATE ENTITY] #%d ⚠️ NaN position for EntityId: %d - Position: %s"),
            CreateEntityCount, EntityId, *Position.ToString()));
        IsValidPosition = false;
    }

    // Verificar posição zero
    bool IsZeroPosition = FMath::IsNearlyZero(Position.X, 0.1f) &&
                          FMath::IsNearlyZero(Position.Y, 0.1f) &&
                          FMath::IsNearlyZero(Position.Z, 0.1f);

    if (IsZeroPosition)
    {
        ClientFileLog(FString::Printf(TEXT("[CREATE ENTITY] #%d ⚠️ Zero position for EntityId: %d - Position: %s"),
            CreateEntityCount, EntityId, *Position.ToString()));

        // Para entidades criadas, não queremos permitir posição zero
        IsValidPosition = false;
    }

    // Se a posição for inválida, tente usar a última posição válida conhecida ou uma posição padrão
    if (!IsValidPosition)
    {
        if (LastValidPositions.Contains(EntityId))
        {
            Position = LastValidPositions[EntityId];
            ClientFileLog(FString::Printf(TEXT("[CREATE ENTITY] #%d ⚠️ Using last valid position for EntityId: %d - Position: %s"),
                CreateEntityCount, EntityId, *Position.ToString()));
        }
        else
        {
            // Se não temos posição válida anterior, use uma posição padrão não-zero
            Position = FVector(100.0f, 100.0f, 100.0f);
            ClientFileLog(FString::Printf(TEXT("[CREATE ENTITY] #%d ⚠️ Using default position for EntityId: %d - Position: %s"),
                CreateEntityCount, EntityId, *Position.ToString()));
        }
    }
    else if (!IsZeroPosition)
    {
        // Atualizar a última posição válida conhecida
        LastValidPositions.Add(EntityId, Position);
    }

    // Criar entidade
    FActorSpawnParameters SpawnParams;
    SpawnParams.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AdjustIfPossibleButAlwaysSpawn;

    ASyncEntity* Entity = World->SpawnActor<ASyncEntity>(EntityClass, Position, Rotator, SpawnParams);

    if (Entity)
    {
        Entity->EntityId = EntityId;
        Entity->SetFlags(static_cast<EEntityState>(Flags));

        // Definir explicitamente a posição e rotação para garantir que esteja correta desde o início
        Entity->SetActorLocation(Position);
        Entity->SetActorRotation(Rotator);

        // Definir explicitamente os alvos de interpolação para a posição atual
        Entity->TargetLocation = Position;
        Entity->TargetRotation = Rotator;

        SpawnedEntities.Add(EntityId, Entity);

        ClientFileLog(FString::Printf(TEXT("[CREATE ENTITY] #%d ✅ Successfully spawned Entity %d at %s"),
            CreateEntityCount, EntityId, *Entity->GetActorLocation().ToString()));
    }
    else
    {
        ClientFileLog(FString::Printf(TEXT("[CREATE ENTITY] #%d ❌ Failed to spawn Entity %d"),
            CreateEntityCount, EntityId));
    }
}

void ATOSPlayerController::HandleUpdateEntity(FUpdateEntityPacket data)
{
    if (!bIsReadyToSync) return;

    FDeltaUpdateData DeltaData{};
    DeltaData.Index = data.EntityId;
    DeltaData.EntitiesMask = EEntityDelta::Position | EEntityDelta::Rotation | EEntityDelta::AnimState | EEntityDelta::Velocity | EEntityDelta::Flags;
    DeltaData.Positon = data.Positon;
    DeltaData.Rotator = data.Rotator;
    DeltaData.Velocity = data.Velocity;
    DeltaData.AnimationState = data.AnimationState;
    DeltaData.Flags = data.Flags;

    if (ASyncEntity** Found = SpawnedEntities.Find(data.EntityId))
    {
        ASyncEntity* Entity = *Found;

        if (!Entity)
            return;

        ApplyDeltaData(Entity, DeltaData);
    }
    else if (EntityClass)
    {
        UWorld* World = GetWorld();

        if (!World)
            return;

        FActorSpawnParameters Params;
        Params.Owner = nullptr;
        Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
        auto NewEntity = World->SpawnActor<ASyncEntity>(EntityClass, data.Positon, data.Rotator, Params);

        if (NewEntity)
        {
            NewEntity->EntityId = data.EntityId;
            ApplyDeltaData(NewEntity, DeltaData);
            SpawnedEntities.Add(data.EntityId, NewEntity);
        }
    }
}

void ATOSPlayerController::HandleUpdateEntityQuantized(FUpdateEntityQuantizedPacket data)
{
    static int32 HandlerCallCount = 0;
    HandlerCallCount++;

    if (HandlerCallCount <= 15)
    {
        ClientFileLog(FString::Printf(TEXT("=== HandleUpdateEntityQuantized #%d ==="), HandlerCallCount));
        ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] 📨 Received EntityId: %d"), data.EntityId));
        ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] 📨 Raw Quantized: X=%d Y=%d Z=%d"), data.QuantizedX, data.QuantizedY, data.QuantizedZ));
        ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] 📨 Raw Quadrant: X=%d Y=%d"), data.QuadrantX, data.QuadrantY));
        ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] 📨 Raw Yaw: %f"), data.Yaw));
        ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] 📨 Raw Velocity: %s"), *data.Velocity.ToString()));
        ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] 📨 bIsReadyToSync: %s"), bIsReadyToSync ? TEXT("true") : TEXT("false")));
        ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] 📨 SpawnedEntities count: %d"), SpawnedEntities.Num()));
    }

    if (!bIsReadyToSync)
    {
        if (HandlerCallCount <= 10)
        {
            ClientFileLog(TEXT("[HANDLER] ❌ Not ready to sync, returning"));
        }
        return;
    }

    // Validar quadrantes - valores muito grandes ou pequenos são provavelmente erros
    const int16 MaxQuadrantValue = 100; // Limite razoável para quadrantes
    bool QuadrantAdjusted = false;
    int16 QuadrantX = data.QuadrantX;
    int16 QuadrantY = data.QuadrantY;

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

    if (QuadrantAdjusted && HandlerCallCount <= 15)
    {
        ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] ⚠️ Quadrante inválido (%d, %d) - Ajustado para (%d, %d)"),
            data.QuadrantX, data.QuadrantY, QuadrantX, QuadrantY));
    }

    // Calcular a posição mundial para spawn ou comparação
    const float Scale = 100.0f; // Match server scale
    const float QuadrantSize = 25600.0f * 4; // Section size * sections per component

    float WorldX = (QuadrantX * QuadrantSize) + (data.QuantizedX * Scale);
    float WorldY = (QuadrantY * QuadrantSize) + (data.QuantizedY * Scale);
    float WorldZ = data.QuantizedZ * Scale;

    FVector WorldPosition = FVector(WorldX, WorldY, WorldZ);
    FRotator WorldRotation = FRotator(0.0f, data.Yaw, 0.0f);

    // Verificar se a posição é válida
    bool IsValidPosition = true;

    // Verificar valores NaN
    if (FMath::IsNaN(WorldPosition.X) || FMath::IsNaN(WorldPosition.Y) || FMath::IsNaN(WorldPosition.Z))
    {
        ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] ⚠️ Posição NaN detectada para Entity %d"), data.EntityId));
        IsValidPosition = false;
    }

    // Verificar se a posição é zero (posição inválida comum)
    bool IsZeroPosition = FMath::IsNearlyZero(WorldPosition.X, 0.1f) &&
                          FMath::IsNearlyZero(WorldPosition.Y, 0.1f) &&
                          FMath::IsNearlyZero(WorldPosition.Z, 0.1f);

    // Posição zero é suspeita, mas não necessariamente inválida para o spawn inicial
    if (IsZeroPosition && HandlerCallCount <= 15)
    {
        ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] ⚠️ Posição zero detectada para Entity %d"), data.EntityId));
    }

    // Se a posição for inválida, tente usar a última posição válida conhecida
    static TMap<int32, FVector> LastValidPositions;

    if (!IsValidPosition)
    {
        if (LastValidPositions.Contains(data.EntityId))
        {
            WorldPosition = LastValidPositions[data.EntityId];
            ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] ⚠️ Usando última posição válida para Entity %d: %s"),
                data.EntityId, *WorldPosition.ToString()));
        }
        else
        {
            // Se não temos posição válida anterior, use uma posição padrão não-zero
            WorldPosition = FVector(100.0f, 100.0f, 100.0f);
            ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] ⚠️ Usando posição padrão para Entity %d: %s"),
                data.EntityId, *WorldPosition.ToString()));
        }
    }
    else if (!IsZeroPosition)
    {
        // Atualizar a última posição válida conhecida
        LastValidPositions.Add(data.EntityId, WorldPosition);
    }

    if (HandlerCallCount <= 15)
    {
        ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] 🧮 Posição mundial calculada: %s"), *WorldPosition.ToString()));
    }

    if (ASyncEntity** Found = SpawnedEntities.Find(data.EntityId))
    {
        ASyncEntity* Entity = *Found;

        if (!Entity)
        {
            if (HandlerCallCount <= 10)
            {
                ClientFileLog(TEXT("[HANDLER] ❌ Entity found but is NULL"));
            }
            return;
        }

        if (HandlerCallCount <= 10)
        {
            ClientFileLog(FString::Printf(TEXT("[HANDLER] ✅ Found existing entity %d, updating..."), data.EntityId));
            ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] 🧮 Entity Current Position: %s"), *Entity->GetActorLocation().ToString()));
        }

        // Para entidades existentes, primeiro definir diretamente a posição para garantir que não fique em (0,0)
        // Isso é especialmente importante para as primeiras atualizações
        static TMap<int32, int32> EntityUpdateCounts;
        int32& UpdateCount = EntityUpdateCounts.FindOrAdd(data.EntityId, 0);
        UpdateCount++;

        if (UpdateCount <= 3 && !IsZeroPosition)
        {
            // Nas primeiras atualizações, forçar a posição diretamente
            Entity->SetActorLocation(WorldPosition);
            Entity->SetActorRotation(WorldRotation);
            ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] 🚀 Forçando posição inicial para Entity %d: %s"),
                data.EntityId, *WorldPosition.ToString()));
        }

        // Use o método de atualização quantizada
        bool IsFalling = (data.Flags & 1) != 0; // Assume bit 0 is IsFalling flag
        Entity->UpdateFromQuantizedNetwork(
            data.QuantizedX, data.QuantizedY, data.QuantizedZ,
            QuadrantX, QuadrantY, data.Yaw,
            data.Velocity, static_cast<uint32>(data.AnimationState), IsFalling
        );

        if (HandlerCallCount <= 15)
        {
            ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] ✅ Entity Updated - New Position: %s"), *Entity->GetActorLocation().ToString()));
        }
    }
    else if (EntityClass)
    {
        if (HandlerCallCount <= 10)
        {
            ClientFileLog(FString::Printf(TEXT("[HANDLER] 🆕 Spawning new entity %d..."), data.EntityId));
        }

        UWorld* World = GetWorld();

        if (!World)
        {
            if (HandlerCallCount <= 10)
            {
                ClientFileLog(TEXT("[HANDLER] ❌ World is NULL"));
            }
            return;
        }

        // Para novas entidades, não permitir spawn em (0,0,0) a menos que seja explicitamente necessário
        if (IsZeroPosition)
        {
            // Se a posição for zero, tente usar uma posição não-zero padrão para o spawn inicial
            WorldPosition = FVector(100.0f, 100.0f, 100.0f);
            ClientFileLog(FString::Printf(TEXT("[HANDLER] ⚠️ Evitando spawn em posição zero para Entity %d, usando: %s"),
                data.EntityId, *WorldPosition.ToString()));
        }

        if (HandlerCallCount <= 10)
        {
            ClientFileLog(FString::Printf(TEXT("[HANDLER] Spawning at world position: %s"), *WorldPosition.ToString()));
            ClientFileLog(FString::Printf(TEXT("[HANDLER] Spawning with rotation: %s"), *WorldRotation.ToString()));
        }

        FActorSpawnParameters Params;
        Params.Owner = nullptr;
        Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
        auto NewEntity = World->SpawnActor<ASyncEntity>(EntityClass, WorldPosition, WorldRotation, Params);

        if (NewEntity)
        {
            NewEntity->EntityId = data.EntityId;

            // Definir diretamente a posição e rotação para garantir que esteja correta desde o início
            NewEntity->SetActorLocation(WorldPosition);
            NewEntity->SetActorRotation(WorldRotation);

            // Definir explicitamente os alvos de interpolação para a posição atual
            NewEntity->TargetLocation = WorldPosition;
            NewEntity->TargetRotation = WorldRotation;

            // Atualizar a última posição válida conhecida
            if (!IsZeroPosition)
            {
                LastValidPositions.Add(data.EntityId, WorldPosition);
            }

            // Agora chame UpdateFromQuantizedNetwork para configurar animações e outras propriedades
            bool IsFalling = (data.Flags & 1) != 0; // Assume bit 0 is IsFalling flag
            NewEntity->UpdateFromQuantizedNetwork(
                data.QuantizedX, data.QuantizedY, data.QuantizedZ,
                QuadrantX, QuadrantY, data.Yaw,
                data.Velocity, static_cast<uint32>(data.AnimationState), IsFalling
            );

            SpawnedEntities.Add(data.EntityId, NewEntity);

            if (HandlerCallCount <= 10)
            {
                ClientFileLog(FString::Printf(TEXT("[HANDLER] ✅ Entity %d spawned at %s and added to SpawnedEntities"),
                    data.EntityId, *NewEntity->GetActorLocation().ToString()));
            }
        }
        else
        {
            if (HandlerCallCount <= 10)
            {
                ClientFileLog(TEXT("[HANDLER] ❌ Failed to spawn entity"));
            }
        }
    }
    else
    {
        if (HandlerCallCount <= 10)
        {
            ClientFileLog(TEXT("[HANDLER] ❌ EntityClass is NULL, cannot spawn"));
        }
    }
}

void ATOSPlayerController::HandleDeltaUpdate(FDeltaUpdateData data)
{
    if (!bIsReadyToSync) return;

    if (ASyncEntity** Found = SpawnedEntities.Find(data.Index))
    {
        ASyncEntity* Entity = *Found;
        if (!Entity) return;
        ApplyDeltaData(Entity, data);
    }
    else if (EntityClass)
    {
        UWorld* World = GetWorld();

        if (!World)
            return;

        FActorSpawnParameters Params;
        Params.Owner = nullptr;
        Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
        auto NewEntity = World->SpawnActor<ASyncEntity>(EntityClass, data.Positon, data.Rotator, Params);

        if (NewEntity)
        {
            NewEntity->EntityId = data.Index;
            ApplyDeltaData(NewEntity, data);
            SpawnedEntities.Add(data.Index, NewEntity);
        }
    }
}

void ATOSPlayerController::ApplyDeltaData(ASyncEntity* Entity, const FDeltaUpdateData& Data)
{
    constexpr float MaxTeleportDistance = 100.0f;
    constexpr float MaxTeleportAngle = 30.0f;

    if (EnumHasAnyFlags(Data.EntitiesMask, EEntityDelta::Position))
    {
        Entity->SetActorLocation(Data.Positon);
        /*const FVector CurrentLocation = Entity->GetActorLocation();
        const float Distance = FVector::Dist(CurrentLocation, Data.Positon);

        if (Distance > MaxTeleportDistance)
        {
            Entity->SetActorLocation(Data.Positon);
            Entity->TargetLocation = Data.Positon;
        }
        else
        {
            Entity->TargetLocation = Data.Positon;
        }*/
    }

    if (EnumHasAnyFlags(Data.EntitiesMask, EEntityDelta::Rotation))
    {
        Entity->SetActorRotation(Data.Rotator);
        /*const FRotator CurrentRotation = Entity->GetActorRotation();
        const float DeltaAngle = FMath::Abs((CurrentRotation - Data.Rotator).GetNormalized().Yaw);

        if (DeltaAngle > MaxTeleportAngle)
        {
            Entity->SetActorRotation(Data.Rotator);
            Entity->TargetRotation = Data.Rotator;
        }
        else
        {
            Entity->TargetRotation = Data.Rotator;
        }*/
    }

    EEntityState Flags = static_cast<EEntityState>(Data.Flags);
    bool bIsFalling = HasEntityState(Flags, EEntityState::IsFalling);
    FVector Velocity = EnumHasAnyFlags(Data.EntitiesMask, EEntityDelta::Velocity) ? Data.Velocity : FVector::ZeroVector;
    uint32 Anim = EnumHasAnyFlags(Data.EntitiesMask, EEntityDelta::AnimState) ? Data.AnimationState : Entity->AnimationState;
    Entity->AnimationState = Anim;
    Entity->UpdateAnimationFromNetwork(Velocity, Anim, bIsFalling);
}

void ATOSPlayerController::HandleRemoveEntity(int32 EntityId)
{
    static int32 RemoveEntityCount = 0;
    RemoveEntityCount++;

    if (!bIsReadyToSync)
    {
        ClientFileLog(FString::Printf(TEXT("[REMOVE ENTITY] #%d ❌ Not ready to sync for EntityId: %d"),
            RemoveEntityCount, EntityId));
        return;
    }

    if (ASyncEntity* const* Found = SpawnedEntities.Find(EntityId))
    {
        ASyncEntity* Entity = *Found;

        if (Entity)
        {
            ClientFileLog(FString::Printf(TEXT("[REMOVE ENTITY] #%d ✅ Removing Entity %d at position %s"),
                RemoveEntityCount, EntityId, *Entity->GetActorLocation().ToString()));
            Entity->Destroy();
        }

        SpawnedEntities.Remove(EntityId);
    }
    else
    {
        ClientFileLog(FString::Printf(TEXT("[REMOVE ENTITY] #%d ⚠️ Entity %d not found for removal"),
            RemoveEntityCount, EntityId));
    }
}
