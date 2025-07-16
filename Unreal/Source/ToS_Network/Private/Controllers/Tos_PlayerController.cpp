#include "Controllers/ToS_PlayerController.h"
#include "Controllers/ToS_GameInstance.h"
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

void ATOSPlayerController::HandleUpdateEntity(int32 EntityId, FVector Positon, FRotator Rotator, int32 AnimationState, int32 Flags)
{
    if (!bIsReadyToSync) return;

    if (ASyncEntity** Found = SpawnedEntities.Find(EntityId))
    {
        ASyncEntity* Entity = *Found;

        if (!Entity)
			return;

        if (UWorld* World = GetWorld())
        {
            float Delta = World->GetDeltaSeconds();
            //FVector NewLocation = FMath::VInterpTo(Entity->GetActorLocation(), Positon, Delta, 10.f);
            //FRotator NewRotation = FMath::RInterpTo(Entity->GetActorRotation(), Rotator, Delta, 10.f);
            Entity->SetActorLocation(Positon);
            Entity->SetActorRotation(Rotator);
        }
        else
        {
            Entity->SetActorLocation(Positon);
            Entity->SetActorRotation(Rotator);
        }

        Entity->AnimationState = AnimationState;
    }
    else if (EntityClass)
    {
        UWorld* World = GetWorld();

        if (!World)
            return;

        FActorSpawnParameters Params;
        Params.Owner = nullptr;
        Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
        auto NewEntity = World->SpawnActor<ASyncEntity>(EntityClass, Positon, Rotator, Params);

        if (NewEntity)
        {
            NewEntity->EntityId = EntityId;
            NewEntity->AnimationState = AnimationState;
            SpawnedEntities.Add(EntityId, NewEntity);
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

