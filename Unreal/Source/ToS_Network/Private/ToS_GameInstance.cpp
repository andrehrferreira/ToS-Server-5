#include "Tos_GameInstance.h"

static bool bENetInitialized = false;

void UTOSGameInstance::Init()
{
    Super::Init();
}

void UTOSGameInstance::Shutdown()
{
    Super::Shutdown();

    bENetInitialized = false;

    if (UENetSubsystem* Socket = GetSubsystem<UENetSubsystem>())
    {
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
            Socket->Connect(ServerIP, ServerPort);
    }    
}