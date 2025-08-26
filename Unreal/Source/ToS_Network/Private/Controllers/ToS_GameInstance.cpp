#include "Controllers/ToS_GameInstance.h"
#include "Controllers/ToS_PlayerController.h"
#include "Engine/World.h"
#include "Async/Async.h"
#include "Config/ClientConfig.h"

static bool bENetInitialized = false;

void UTOSGameInstance::OnPlayerControllerReady(ATOSPlayerController* Controller)
{
    PlayerController = Controller;
}

void UTOSGameInstance::Init()
{
    Super::Init();

    // Load default configuration first
    LoadDefaultConfiguration();

    if (UENetSubsystem* Socket = GetSubsystem<UENetSubsystem>())
    {
        Socket->OnCreateEntity.AddDynamic(this, &UTOSGameInstance::HandleCreateEntity);
        Socket->OnUpdateEntity.AddDynamic(this, &UTOSGameInstance::HandleUpdateEntity);
        Socket->OnRemoveEntity.AddDynamic(this, &UTOSGameInstance::HandleRemoveEntity);
        Socket->OnDeltaUpdate.AddDynamic(this, &UTOSGameInstance::HandleDeltaUpdate);
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
        Socket->OnDeltaUpdate.RemoveDynamic(this, &UTOSGameInstance::HandleDeltaUpdate);
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

void UTOSGameInstance::HandleDeltaUpdate(FDeltaUpdateData data)
{
    if (!PlayerController)
        return;

    AsyncTask(ENamedThreads::GameThread, [this, data]()
    {
        if (PlayerController)
        {
            PlayerController->HandleDeltaUpdate(data);
        }
    });
}

void UTOSGameInstance::LoadDefaultConfiguration()
{
    UClientConfig* DefaultConfig = UClientConfig::GetDefaultConfig();
    if (DefaultConfig)
    {
        ApplyClientConfiguration(DefaultConfig);
        UE_LOG(LogTemp, Warning, TEXT("Default client configuration loaded"));
    }
    else
    {
        UE_LOG(LogTemp, Error, TEXT("Failed to load default client configuration"));
    }
}

void UTOSGameInstance::ApplyClientConfiguration(UClientConfig* Config)
{
    if (!Config)
    {
        UE_LOG(LogTemp, Error, TEXT("ClientConfig is null"));
        return;
    }

    if (!Config->ValidateConfiguration())
    {
        UE_LOG(LogTemp, Error, TEXT("Client configuration validation failed"));
        return;
    }

    // Apply network settings
    ServerIP = Config->Network.ServerIP;
    ServerPort = Config->Network.ServerPort;
    ServerAutoConnect = Config->Network.bAutoConnect;

    // Apply security settings
    bEnableEndToEndEncryption = Config->Security.bEnableEndToEndEncryption;
    bEnableIntegrityCheck = Config->Security.bEnableIntegrityCheck;
    bEnableLZ4Compression = Config->Security.bEnableLZ4Compression;
    CompressionThreshold = Config->Security.CompressionThreshold;
    ServerPassword = Config->Security.ServerPassword;

    // Apply performance settings
    SendRateHz = Config->Performance.SendRateHz;
    MaxPacketSize = Config->Performance.MaxPacketSize;
    ReliableTimeoutMs = Config->Performance.ReliableTimeoutMs;
    MaxRetries = Config->Performance.MaxRetries;

    // Apply logging settings
    bEnableDebugLogs = Config->Logging.bEnableDebugLogs;
    bEnableFileLogging = Config->Logging.bEnableFileLogging;
    bEnableCryptoLogs = Config->Logging.bEnableCryptoLogs;
    bEnablePacketLogs = Config->Logging.bEnablePacketLogs;

    // Apply settings to ENetSubsystem if it exists
    if (UENetSubsystem* NetSubsystem = GetSubsystem<UENetSubsystem>())
    {
        NetSubsystem->SetConnectTimeout(Config->Network.ConnectionTimeoutSeconds);
        NetSubsystem->SetRetryInterval(Config->Network.RetryIntervalSeconds);
        NetSubsystem->SetRetryEnabled(Config->Network.bEnableRetry);
    }

    UE_LOG(LogTemp, Warning, TEXT("Client configuration applied successfully"));
    UE_LOG(LogTemp, Warning, TEXT("Network: %s:%d (AutoConnect: %s)"), *ServerIP, ServerPort, ServerAutoConnect ? TEXT("Yes") : TEXT("No"));
    UE_LOG(LogTemp, Warning, TEXT("Security: Encryption=%s, Compression=%s, Integrity=%s"),
        bEnableEndToEndEncryption ? TEXT("On") : TEXT("Off"),
        bEnableLZ4Compression ? TEXT("On") : TEXT("Off"),
        bEnableIntegrityCheck ? TEXT("On") : TEXT("Off"));
    UE_LOG(LogTemp, Warning, TEXT("Performance: SendRate=%dHz, MaxPacket=%d bytes"), SendRateHz, MaxPacketSize);
}

bool UTOSGameInstance::ValidateConfiguration() const
{
    // Validate network settings
    if (ServerPort <= 0 || ServerPort > 65535)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid server port: %d"), ServerPort);
        return false;
    }

    if (ServerIP.IsEmpty())
    {
        UE_LOG(LogTemp, Error, TEXT("Server IP is empty"));
        return false;
    }

    // Validate performance settings
    if (SendRateHz <= 0 || SendRateHz > 120)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid send rate: %d"), SendRateHz);
        return false;
    }

    if (MaxPacketSize < 512 || MaxPacketSize > 8192)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid max packet size: %d"), MaxPacketSize);
        return false;
    }

    if (ReliableTimeoutMs < 50 || ReliableTimeoutMs > 5000)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid reliable timeout: %d"), ReliableTimeoutMs);
        return false;
    }

    if (MaxRetries <= 0 || MaxRetries > 50)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid max retries: %d"), MaxRetries);
        return false;
    }

    return true;
}
