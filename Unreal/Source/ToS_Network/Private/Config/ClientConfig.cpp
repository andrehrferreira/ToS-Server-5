#include "Config/ClientConfig.h"
#include "Controllers/ToS_GameInstance.h"
#include "Engine/Engine.h"

UClientConfig* UClientConfig::DefaultConfigInstance = nullptr;

UClientConfig* UClientConfig::GetDefaultConfig()
{
    if (!DefaultConfigInstance)
    {
        DefaultConfigInstance = NewObject<UClientConfig>();

        // Set default values
        DefaultConfigInstance->Network.ServerIP = TEXT("127.0.0.1");
        DefaultConfigInstance->Network.ServerPort = 3565;
        DefaultConfigInstance->Network.bAutoConnect = true;
        DefaultConfigInstance->Network.ConnectionTimeoutSeconds = 10.0f;
        DefaultConfigInstance->Network.RetryIntervalSeconds = 5.0f;
        DefaultConfigInstance->Network.bEnableRetry = true;

        DefaultConfigInstance->Security.bEnableEndToEndEncryption = true;
        DefaultConfigInstance->Security.bEnableIntegrityCheck = true;
        DefaultConfigInstance->Security.bEnableLZ4Compression = true;
        DefaultConfigInstance->Security.CompressionThreshold = 512;
        DefaultConfigInstance->Security.ServerPassword = TEXT("");
        DefaultConfigInstance->Security.bEnableAntiCheat = false;

        DefaultConfigInstance->Performance.SendRateHz = 20;
        DefaultConfigInstance->Performance.MaxPacketSize = 1200;
        DefaultConfigInstance->Performance.bEnablePerformanceMetrics = false;
        DefaultConfigInstance->Performance.ReliableTimeoutMs = 250;
        DefaultConfigInstance->Performance.MaxRetries = 10;

        DefaultConfigInstance->Logging.bEnableDebugLogs = false;
        DefaultConfigInstance->Logging.bEnableFileLogging = true;
        DefaultConfigInstance->Logging.LogLevel = TEXT("Info");
        DefaultConfigInstance->Logging.bEnableCryptoLogs = false;
        DefaultConfigInstance->Logging.bEnablePacketLogs = false;
    }

    return DefaultConfigInstance;
}

UClientConfig* UClientConfig::LoadConfigFromSettings()
{
    // For now, return default config
    // In the future, this could load from project settings or a config file
    return GetDefaultConfig();
}

bool UClientConfig::ValidateConfiguration() const
{
    // Validate network settings
    if (Network.ServerPort <= 0 || Network.ServerPort > 65535)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid server port: %d"), Network.ServerPort);
        return false;
    }

    if (Network.ConnectionTimeoutSeconds <= 0.0f || Network.ConnectionTimeoutSeconds > 60.0f)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid connection timeout: %f"), Network.ConnectionTimeoutSeconds);
        return false;
    }

    if (Network.RetryIntervalSeconds <= 0.0f || Network.RetryIntervalSeconds > 30.0f)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid retry interval: %f"), Network.RetryIntervalSeconds);
        return false;
    }

    // Validate security settings
    if (Security.CompressionThreshold < 100 || Security.CompressionThreshold > 2048)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid compression threshold: %d"), Security.CompressionThreshold);
        return false;
    }

    // Validate performance settings
    if (Performance.SendRateHz <= 0 || Performance.SendRateHz > 120)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid send rate: %d"), Performance.SendRateHz);
        return false;
    }

    if (Performance.MaxPacketSize < 512 || Performance.MaxPacketSize > 8192)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid max packet size: %d"), Performance.MaxPacketSize);
        return false;
    }

    if (Performance.ReliableTimeoutMs < 50 || Performance.ReliableTimeoutMs > 5000)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid reliable timeout: %d"), Performance.ReliableTimeoutMs);
        return false;
    }

    if (Performance.MaxRetries <= 0 || Performance.MaxRetries > 50)
    {
        UE_LOG(LogTemp, Error, TEXT("Invalid max retries: %d"), Performance.MaxRetries);
        return false;
    }

    return true;
}

void UClientConfig::ApplyToGameInstance(UTOSGameInstance* GameInstance) const
{
    if (!GameInstance)
    {
        UE_LOG(LogTemp, Error, TEXT("GameInstance is null"));
        return;
    }

    if (!ValidateConfiguration())
    {
        UE_LOG(LogTemp, Error, TEXT("Configuration validation failed"));
        return;
    }

    // Apply network settings
    GameInstance->ServerIP = Network.ServerIP;
    GameInstance->ServerPort = Network.ServerPort;
    GameInstance->ServerAutoConnect = Network.bAutoConnect;

    // Apply security settings
    GameInstance->bEnableEndToEndEncryption = Security.bEnableEndToEndEncryption;
    GameInstance->bEnableIntegrityCheck = Security.bEnableIntegrityCheck;
    GameInstance->bEnableLZ4Compression = Security.bEnableLZ4Compression;
    GameInstance->CompressionThreshold = Security.CompressionThreshold;
    GameInstance->ServerPassword = Security.ServerPassword;

    // Apply performance settings
    GameInstance->SendRateHz = Performance.SendRateHz;
    GameInstance->MaxPacketSize = Performance.MaxPacketSize;
    GameInstance->ReliableTimeoutMs = Performance.ReliableTimeoutMs;
    GameInstance->MaxRetries = Performance.MaxRetries;

    // Apply logging settings
    GameInstance->bEnableDebugLogs = Logging.bEnableDebugLogs;
    GameInstance->bEnableFileLogging = Logging.bEnableFileLogging;
    GameInstance->bEnableCryptoLogs = Logging.bEnableCryptoLogs;
    GameInstance->bEnablePacketLogs = Logging.bEnablePacketLogs;

    UE_LOG(LogTemp, Warning, TEXT("Client configuration applied successfully"));
}
