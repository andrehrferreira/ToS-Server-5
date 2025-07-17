#include "Controllers/ToS_PlayerController.h"
#include "Controllers/ToS_GameInstance.h"
#include "Enum/EntityState.h"
#include "Enum/EntityDelta.h"
#include "Engine/World.h"

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

void ATOSPlayerController::HandleCreateEntity(int32 EntityId, FVector Positon, FRotator Rotator, int32 Flags)
{
    if (!bIsReadyToSync) return;

    if (!EntityClass) return;

    UWorld* World = GetWorld();
    if (!World) return;

    FActorSpawnParameters Params;
    Params.Owner = nullptr;
    Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
    ASyncEntity* NewEntity = World->SpawnActor<ASyncEntity>(EntityClass, Positon, Rotator, Params);

    if (NewEntity)
    {
        NewEntity->EntityId = EntityId;
        SpawnedEntities.Add(EntityId, NewEntity);
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
        const FVector CurrentLocation = Entity->GetActorLocation();
        const float Distance = FVector::Dist(CurrentLocation, Data.Positon);

        if (Distance > MaxTeleportDistance)
        {
            Entity->SetActorLocation(Data.Positon);
            Entity->TargetLocation = Data.Positon;
        }
        else
        {
            Entity->TargetLocation = Data.Positon;
        }
    }

    if (EnumHasAnyFlags(Data.EntitiesMask, EEntityDelta::Rotation))
    {
        const FRotator CurrentRotation = Entity->GetActorRotation();
        const float DeltaAngle = FMath::Abs((CurrentRotation - Data.Rotator).GetNormalized().Yaw);

        if (DeltaAngle > MaxTeleportAngle)
        {
            Entity->SetActorRotation(Data.Rotator);
            Entity->TargetRotation = Data.Rotator;
        }
        else
        {
            Entity->TargetRotation = Data.Rotator;
        }
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
    if (!bIsReadyToSync) return;

    if (ASyncEntity* const* Found = SpawnedEntities.Find(EntityId))
    {
        ASyncEntity* Entity = *Found;

        if (Entity)
            Entity->Destroy();

        SpawnedEntities.Remove(EntityId);
    }
}

