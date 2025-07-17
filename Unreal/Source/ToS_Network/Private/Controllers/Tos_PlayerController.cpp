#include "Controllers/ToS_PlayerController.h"
#include "Controllers/ToS_GameInstance.h"
#include "Enum/EntityState.h"
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

    if (ASyncEntity** Found = SpawnedEntities.Find(data.EntityId))
    {
        ASyncEntity* Entity = *Found;

        EEntityState Flags = static_cast<EEntityState>(data.Flags);

        if (!Entity)
			return;

        if (UWorld* World = GetWorld())
        {
            float Delta = World->GetDeltaSeconds();
            Entity->SetActorLocation(data.Positon);
            Entity->SetActorRotation(data.Rotator);
        }
        else
        {
            Entity->SetActorLocation(data.Positon);
            Entity->SetActorRotation(data.Rotator);
        }

        Entity->AnimationState = data.AnimationState;
        Entity->UpdateAnimationFromNetwork(data.Velocity, data.AnimationState, HasEntityState(Flags, EEntityState::IsFalling));
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
            NewEntity->AnimationState = data.AnimationState;
            NewEntity->UpdateAnimationFromNetwork(data.Velocity, data.AnimationState, false);
            SpawnedEntities.Add(data.EntityId, NewEntity);
        }
    }
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

