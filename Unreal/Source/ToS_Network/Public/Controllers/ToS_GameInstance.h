#pragma once

#include "CoreMinimal.h"
#include "Engine/GameInstance.h"
#include "Network/ENetSubsystem.h"
#include "Entities/SyncEntity.h"
#include "Entities/SyncPlayer.h"
#include "Config/ClientConfig.h"
#include "Tos_GameInstance.generated.h"

class ATOSPlayerController;

UCLASS()
class TOS_NETWORK_API UTOSGameInstance : public UGameInstance
{
    GENERATED_BODY()

public:
    virtual void Init() override;
    virtual void OnStart() override;
    virtual void Shutdown() override;

    void OnPlayerControllerReady(ATOSPlayerController* Controller);

    UFUNCTION(BlueprintImplementableEvent, Category = "GameInstance")
    void OnGameInstanceStarted();

    // === NETWORK CONFIGURATION ===
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network")
    bool ServerAutoConnect = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network")
    FString ServerIP = TEXT("127.0.0.1");

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network")
    int32 ServerPort = 3565;

    // === SECURITY CONFIGURATION ===
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

    // === PERFORMANCE CONFIGURATION ===
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Performance", meta = (DisplayName = "Send Rate (Hz)"))
    int32 SendRateHz = 20;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Performance", meta = (DisplayName = "Max Packet Size (bytes)"))
    int32 MaxPacketSize = 1200;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Performance", meta = (DisplayName = "Reliable Timeout (ms)"))
    int32 ReliableTimeoutMs = 250;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Performance", meta = (DisplayName = "Max Retries"))
    int32 MaxRetries = 10;

    // === LOGGING CONFIGURATION ===
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Logging", meta = (DisplayName = "Enable Debug Logs"))
    bool bEnableDebugLogs = false;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Logging", meta = (DisplayName = "Enable File Logging"))
    bool bEnableFileLogging = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Logging", meta = (DisplayName = "Enable Crypto Logs"))
    bool bEnableCryptoLogs = false;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Logging", meta = (DisplayName = "Enable Packet Logs"))
    bool bEnablePacketLogs = false;

    // === WORLD ORIGIN REBASING CONFIGURATION ===
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "World Origin Rebasing", meta = (DisplayName = "Enable World Origin Rebasing"))
    bool bEnableWorldOriginRebasing = false;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "World Origin Rebasing", meta = (DisplayName = "Enable Yaw Only Rotation"))
    bool bEnableYawOnlyRotation = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "World Origin Rebasing", meta = (DisplayName = "Current Map Name"))
    FString CurrentMapName = TEXT("SurvivalMap");

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "World Origin Rebasing", meta = (DisplayName = "Current Quadrant X"))
    int32 CurrentQuadrantX = 0;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "World Origin Rebasing", meta = (DisplayName = "Current Quadrant Y"))
    int32 CurrentQuadrantY = 0;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Entities")
    TSubclassOf<ASyncEntity> EntityClass;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Entities")
    TSubclassOf<ASyncPlayer> PlayerClass;

private:
    UPROPERTY()
    ATOSPlayerController* PlayerController = nullptr;

    UFUNCTION()
    void HandleCreateEntity(int32 EntityId, FVector Positon, FRotator Rotator, int32 Flags);

    UFUNCTION()
    void HandleUpdateEntity(FUpdateEntityPacket data);

    UFUNCTION()
    void HandleRemoveEntity(int32 EntityId);

    UFUNCTION()
    void HandleDeltaUpdate(FDeltaUpdateData data);

    UFUNCTION()
    void HandleUpdateEntityQuantized(FUpdateEntityQuantizedPacket data);

    // === CONFIGURATION METHODS ===
    UFUNCTION(BlueprintCallable, Category = "Configuration")
    void LoadDefaultConfiguration();

    UFUNCTION(BlueprintCallable, Category = "Configuration")
    void ApplyClientConfiguration(UClientConfig* Config);

    UFUNCTION(BlueprintCallable, Category = "Configuration")
    bool ValidateConfiguration() const;
};

