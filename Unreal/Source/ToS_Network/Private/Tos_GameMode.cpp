#include "Tos_GameMode.h"
#include "Engine/World.h"

ASyncEntity* ATOSGameMode::GetEntityById(int32 Id)
{
    if (ASyncEntity** Found = SpawnedEntities.Find(Id))
    {
        return *Found;
    }

    return nullptr;
}

void ATOSGameMode::HandleCreateEntity(int32 EntityId, FVector Positon, FRotator Rotator, int32 Flags)
{
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

void ATOSGameMode::HandleUpdateEntity(int32 EntityId, FVector Positon, FRotator Rotator, int32 AnimationState, int32 Flags)
{
    if (ASyncEntity** Found = SpawnedEntities.Find(EntityId))
    {
        ASyncEntity* Entity = *Found;
        if (UWorld* World = GetWorld())
        {
            float Delta = World->GetDeltaSeconds();
            FVector NewLocation = FMath::VInterpTo(Entity->GetActorLocation(), Positon, Delta, 10.f);
            FRotator NewRotation = FMath::RInterpTo(Entity->GetActorRotation(), Rotator, Delta, 10.f);
            Entity->SetActorLocation(NewLocation);
            Entity->SetActorRotation(NewRotation);
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
        ASyncEntity* NewEntity = World->SpawnActor<ASyncEntity>(EntityClass, Positon, Rotator, Params);

        if (NewEntity)
        {
            NewEntity->EntityId = EntityId;
            NewEntity->AnimationState = AnimationState;
            SpawnedEntities.Add(EntityId, NewEntity);
        }
    }
}

void ATOSGameMode::HandleRemoveEntity(int32 EntityId)
{
    if (ASyncEntity* const* Found = SpawnedEntities.Find(EntityId))
    {
        ASyncEntity* Entity = *Found;

        if (Entity)
            Entity->Destroy();

        SpawnedEntities.Remove(EntityId);
    }
}

