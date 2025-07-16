#include "Controllers/ToS_GameInstance.h"
#include "Controllers/ToS_PlayerController.h"
#include "Engine/World.h"
#include "Async/Async.h"

static bool bENetInitialized = false;

void UTOSGameInstance::OnPlayerControllerReady(ATOSPlayerController* Controller)
{
    PlayerController = Controller;
}

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
        {
            Socket->Connect(ServerIP, ServerPort);
        }
    }
}

void UTOSGameInstance::HandleCreateEntity(int32 EntityId, FVector Positon, FRotator Rotator, int32 Flags)
{
    if (!PlayerController)
        return;

    AsyncTask(ENamedThreads::GameThread, [this, EntityId, Positon, Rotator, Flags]()
    {
        if (PlayerController)
        {
            PlayerController->HandleCreateEntity(EntityId, Positon, Rotator, Flags);
        }
    });
}

void UTOSGameInstance::HandleUpdateEntity(FUpdateEntityPacket data)
{
    if (!PlayerController)
        return;

    AsyncTask(ENamedThreads::GameThread, [this, data]()
    {
        if (PlayerController)
        {
            PlayerController->HandleUpdateEntity(data);
        }
    });
}

void UTOSGameInstance::HandleRemoveEntity(int32 EntityId)
{
    if (!PlayerController)
        return;

    AsyncTask(ENamedThreads::GameThread, [this, EntityId]()
    {
        if (PlayerController)
        {
            PlayerController->HandleRemoveEntity(EntityId);
        }
    });
}
