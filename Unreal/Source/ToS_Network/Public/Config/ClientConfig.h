#pragma once

#include "CoreMinimal.h"
#include "Engine/DataAsset.h"
#include "ClientConfig.generated.h"

USTRUCT(BlueprintType)
struct FNetworkClientConfig
{
    GENERATED_USTRUCT_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network", meta = (DisplayName = "Server IP"))
    FString ServerIP = TEXT("127.0.0.1");

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network", meta = (DisplayName = "Server Port"))
    int32 ServerPort = 3565;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network", meta = (DisplayName = "Auto Connect"))
    bool bAutoConnect = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network", meta = (DisplayName = "Connection Timeout (seconds)"))
    float ConnectionTimeoutSeconds = 10.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network", meta = (DisplayName = "Retry Interval (seconds)"))
    float RetryIntervalSeconds = 5.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network", meta = (DisplayName = "Enable Retry"))
    bool bEnableRetry = true;
};

USTRUCT(BlueprintType)
struct FSecurityClientConfig
{
    GENERATED_USTRUCT_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Security", meta = (DisplayName = "Enable End-to-End Encryption"))
    bool bEnableEndToEndEncryption = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Security", meta = (DisplayName = "Enable Integrity Check"))
    bool bEnableIntegrityCheck = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Security", meta = (DisplayName = "Enable LZ4 Compression"))
    bool bEnableLZ4Compression = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Security", meta = (DisplayName = "Compression Threshold (bytes)"))
    int32 CompressionThreshold = 512;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Security", meta = (DisplayName = "Server Password"))
    FString ServerPassword = TEXT("");

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Security", meta = (DisplayName = "Enable Anti-Cheat"))
    bool bEnableAntiCheat = false;
};

USTRUCT(BlueprintType)
struct FPerformanceClientConfig
{
    GENERATED_USTRUCT_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Performance", meta = (DisplayName = "Send Rate (Hz)"))
    int32 SendRateHz = 20;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Performance", meta = (DisplayName = "Max Packet Size (bytes)"))
    int32 MaxPacketSize = 1200;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Performance", meta = (DisplayName = "Enable Performance Metrics"))
    bool bEnablePerformanceMetrics = false;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Performance", meta = (DisplayName = "Reliable Timeout (ms)"))
    int32 ReliableTimeoutMs = 250;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Performance", meta = (DisplayName = "Max Retries"))
    int32 MaxRetries = 10;
};

USTRUCT(BlueprintType)
struct FLoggingClientConfig
{
    GENERATED_USTRUCT_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Logging", meta = (DisplayName = "Enable Debug Logs"))
    bool bEnableDebugLogs = false;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Logging", meta = (DisplayName = "Enable File Logging"))
    bool bEnableFileLogging = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Logging", meta = (DisplayName = "Log Level"))
    FString LogLevel = TEXT("Info");

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Logging", meta = (DisplayName = "Enable Crypto Logs"))
    bool bEnableCryptoLogs = false;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Logging", meta = (DisplayName = "Enable Packet Logs"))
    bool bEnablePacketLogs = false;
};

/**
 * Client configuration data asset
 */
UCLASS(BlueprintType)
class TOS_NETWORK_API UClientConfig : public UDataAsset
{
    GENERATED_BODY()

public:
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network Configuration")
    FNetworkClientConfig Network;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Security Configuration")
    FSecurityClientConfig Security;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Performance Configuration")
    FPerformanceClientConfig Performance;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Logging Configuration")
    FLoggingClientConfig Logging;

    /** Get default client configuration */
    UFUNCTION(BlueprintCallable, Category = "Configuration")
    static UClientConfig* GetDefaultConfig();

    /** Load configuration from project settings */
    UFUNCTION(BlueprintCallable, Category = "Configuration")
    static UClientConfig* LoadConfigFromSettings();

    /** Validate configuration values */
    UFUNCTION(BlueprintCallable, Category = "Configuration")
    bool ValidateConfiguration() const;

    /** Apply configuration to game instance */
    UFUNCTION(BlueprintCallable, Category = "Configuration")
    void ApplyToGameInstance(class UTOSGameInstance* GameInstance) const;

private:
    static UClientConfig* DefaultConfigInstance;
};
