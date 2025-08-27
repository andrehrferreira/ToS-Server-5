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

    // Validar posição para evitar valores inválidos
    if (FMath::IsNaN(Position.X) || FMath::IsNaN(Position.Y) || FMath::IsNaN(Position.Z))
    {
        ClientFileLog(FString::Printf(TEXT("[CREATE ENTITY] #%d ❌ Invalid position for EntityId: %d - Position: %s"),
            CreateEntityCount, EntityId, *Position.ToString()));
        return;
    }

    // Criar entidade
    FActorSpawnParameters SpawnParams;
    SpawnParams.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AdjustIfPossibleButAlwaysSpawn;

    ASyncEntity* Entity = World->SpawnActor<ASyncEntity>(EntityClass, Position, Rotator, SpawnParams);

    if (Entity)
    {
        Entity->EntityId = EntityId;
        Entity->SetFlags(static_cast<EEntityState>(Flags));
        SpawnedEntities.Add(EntityId, Entity);

        ClientFileLog(FString::Printf(TEXT("[CREATE ENTITY] #%d ✅ Successfully spawned Entity %d at %s"),
            CreateEntityCount, EntityId, *Position.ToString()));
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
        }

        // Calculate final world position before updating (for comparison)
        if (HandlerCallCount <= 15)
        {
            const float Scale = 100.0f;
            const float QuadrantSize = 25600.0f * 4;
            float WorldX = (data.QuadrantX * QuadrantSize) + (data.QuantizedX * Scale);
            float WorldY = (data.QuadrantY * QuadrantSize) + (data.QuantizedY * Scale);
            float WorldZ = data.QuantizedZ * Scale;
            FVector CalculatedWorldPos = FVector(WorldX, WorldY, WorldZ);

            ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] 🧮 Calculated World Position: %s"), *CalculatedWorldPos.ToString()));
            ClientFileLog(FString::Printf(TEXT("[CLIENT RECV] 🧮 Entity Current Position: %s"), *Entity->GetActorLocation().ToString()));
        }

        // Use the new quantized update method
        bool IsFalling = (data.Flags & 1) != 0; // Assume bit 0 is IsFalling flag
        Entity->UpdateFromQuantizedNetwork(
            data.QuantizedX, data.QuantizedY, data.QuantizedZ,
            data.QuadrantX, data.QuadrantY, data.Yaw,
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

        // Convert quantized position back to world position for spawning
        const float Scale = 100.0f; // Match server scale
        const float QuadrantSize = 25600.0f * 4; // Section size * sections per component

        float WorldX = (data.QuadrantX * QuadrantSize) + (data.QuantizedX * Scale);
        float WorldY = (data.QuadrantY * QuadrantSize) + (data.QuantizedY * Scale);
        float WorldZ = data.QuantizedZ * Scale;

        FVector SpawnPosition = FVector(WorldX, WorldY, WorldZ);
        FRotator SpawnRotation = FRotator(0.0f, data.Yaw, 0.0f);

        if (HandlerCallCount <= 10)
        {
            ClientFileLog(FString::Printf(TEXT("[HANDLER] Spawning at world position: %s"), *SpawnPosition.ToString()));
            ClientFileLog(FString::Printf(TEXT("[HANDLER] Spawning with rotation: %s"), *SpawnRotation.ToString()));
        }

        FActorSpawnParameters Params;
        Params.Owner = nullptr;
        Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
        auto NewEntity = World->SpawnActor<ASyncEntity>(EntityClass, SpawnPosition, SpawnRotation, Params);

        if (NewEntity)
        {
            NewEntity->EntityId = data.EntityId;
            bool IsFalling = (data.Flags & 1) != 0; // Assume bit 0 is IsFalling flag
            NewEntity->UpdateFromQuantizedNetwork(
                data.QuantizedX, data.QuantizedY, data.QuantizedZ,
                data.QuadrantX, data.QuadrantY, data.Yaw,
                data.Velocity, static_cast<uint32>(data.AnimationState), IsFalling
            );
            SpawnedEntities.Add(data.EntityId, NewEntity);

            if (HandlerCallCount <= 10)
            {
                ClientFileLog(FString::Printf(TEXT("[HANDLER] ✅ Entity %d spawned and added to SpawnedEntities"), data.EntityId));
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
