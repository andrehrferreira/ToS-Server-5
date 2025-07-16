#pragma once

#include "CoreMinimal.h"
#include "GameFramework/PlayerController.h"
#include "Entities/SyncEntity.h"
#include "Entities/SyncPlayer.h"
#include "Tos_PlayerController.generated.h"

UCLASS()
class TOS_NETWORK_API ATOSPlayerController : public APlayerController
{
    GENERATED_BODY()

public:
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Entities")
    TSubclassOf<ASyncEntity> EntityClass;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Entities")
    TSubclassOf<ASyncPlayer> PlayerClass;

    UPROPERTY(BlueprintReadOnly, Category = "Entities")
    TMap<int32, ASyncEntity*> SpawnedEntities;

    UFUNCTION(BlueprintCallable, Category = "Entities")
    ASyncEntity* GetEntityById(int32 Id);

    UFUNCTION()
    void HandleCreateEntity(int32 EntityId, FVector Positon, FRotator Rotator, int32 Flags);

    UFUNCTION()
    void HandleUpdateEntity(int32 EntityId, FVector Positon, FRotator Rotator, int32 AnimationState, int32 Flags);

    UFUNCTION()
    void HandleRemoveEntity(int32 EntityId);
};

