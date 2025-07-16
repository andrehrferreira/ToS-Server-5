#include "Tos_GameInstance.h"
#include "Entities/SyncEntity.h"
#include "Engine/World.h"

static bool bENetInitialized = false;

void UTOSGameInstance::Init()
{
    Super::Init();

    if (UENetSubsystem* Socket = GetSubsystem<UENetSubsystem>())
    {
        Socket->OnCreateEntity.AddDynamic(this, &UTOSGameInstance::HandleCreateEntity);
        Socket->OnUpdateEntity.AddDynamic(this, &UTOSGameInstance::HandleUpdateEntity);
        Socket->OnRemoveEntity.AddDynamic(this, &UTOSGameInstance::HandleRemoveEntity);
    }
}

void UTOSGameInstance::Shutdown()
{
    Super::Shutdown();

    bENetInitialized = false;

    if (UENetSubsystem* Socket = GetSubsystem<UENetSubsystem>())
    {
        Socket->OnCreateEntity.RemoveDynamic(this, &UTOSGameInstance::HandleCreateEntity);
        Socket->OnUpdateEntity.RemoveDynamic(this, &UTOSGameInstance::HandleUpdateEntity);
        Socket->OnRemoveEntity.RemoveDynamic(this, &UTOSGameInstance::HandleRemoveEntity);
        Socket->Disconnect();
    }

    SpawnedEntities.Empty();
}

void UTOSGameInstance::OnStart()
{
    Super::OnStart();

    OnGameInstanceStarted();

    if (UENetSubsystem* Socket = GetSubsystem<UENetSubsystem>())
    {
        if (bENetInitialized)        
            return;
        
        bENetInitialized = true;

        if (!Socket->IsConnected() && Socket->GetConnectionStatus() != EConnectionStatus::Connecting && ServerAutoConnect)
            Socket->Connect(ServerIP, ServerPort);
    }
}

ASyncEntity* UTOSGameInstance::GetEntityById(int32 Id) 
{
    if (ASyncEntity** Found = SpawnedEntities.Find(Id))
    {
        return *Found;
    }

    return nullptr;
}

void UTOSGameInstance::HandleCreateEntity(int32 EntityId, FVector Positon, FRotator Rotator, int32 Flags)
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

void UTOSGameInstance::HandleUpdateEntity(int32 EntityId, FVector Positon, FRotator Rotator, int32 AnimationState, int32 Flags)
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
}

void UTOSGameInstance::HandleRemoveEntity(int32 EntityId)
{
    if (ASyncEntity* const* Found = SpawnedEntities.Find(EntityId))
    {
        ASyncEntity* Entity = *Found;

        if (Entity)        
            Entity->Destroy();
        
        SpawnedEntities.Remove(EntityId);
    }
}
