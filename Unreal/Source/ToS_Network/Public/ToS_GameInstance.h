#pragma once

#include "CoreMinimal.h"
#include "Engine/GameInstance.h"
#include "Network/ENetSubsystem.h"
#include "Entities/SyncEntity.h"
#include "Tos_GameInstance.generated.h"

UCLASS()
class TOS_NETWORK_API UTOSGameInstance : public UGameInstance
{
	GENERATED_BODY()

public:
    virtual void Init() override;
    virtual void OnStart() override;
    virtual void Shutdown() override;

    UFUNCTION(BlueprintImplementableEvent, Category = "GameInstance")
    void OnGameInstanceStarted();

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network")
    bool ServerAutoConnect = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network")
    FString ServerIP = TEXT("127.0.0.1");

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Network")
    int32 ServerPort = 3565;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Entities")
    TSubclassOf<ASyncEntity> EntityClass;

    UPROPERTY(BlueprintReadOnly, Category = "Entities")
    TMap<int32, ASyncEntity*> SpawnedEntities;

    UFUNCTION(BlueprintCallable, Category = "Entities")
    ASyncEntity* GetEntityById(int32 Id) const;

private:
    UFUNCTION()
    void HandleCreateEntity(int32 EntityId, FVector Positon, FRotator Rotator, int32 Flags);

    UFUNCTION()
    void HandleUpdateEntity(int32 EntityId, FVector Positon, FRotator Rotator, int32 AnimationState, int32 Flags);

    UFUNCTION()
    void HandleRemoveEntity(int32 EntityId);
};
