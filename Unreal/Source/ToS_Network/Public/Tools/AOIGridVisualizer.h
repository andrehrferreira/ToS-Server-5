// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "AOIGridVisualizer.generated.h"

UCLASS()
class TOS_NETWORK_API AAOIGridVisualizer : public AActor
{
    GENERATED_BODY()

public:
    AAOIGridVisualizer();

protected:
    virtual void BeginPlay() override;
    virtual void OnConstruction(const FTransform& Transform) override;
    virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;
	virtual void Destroyed() override;

public:
    virtual void Tick(float DeltaTime) override;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Grid Settings")
    FVector CellSize = FVector(500, 500, 500);

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Grid Settings")
    int32 GridWidth = 10;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Grid Settings")
    int32 GridHeight = 10;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Grid Settings")
    int32 GridDepth = 4;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Grid Settings")
    FColor GridColor = FColor::Green;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Grid Settings")
    TSet<FIntVector> ActiveCells;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Grid Settings")
    FColor ActiveCellColor = FColor::Red;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "AOI")
    FColor InterestAreaColor = FColor::Yellow;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "AOI")
    AActor* PlayerActor = nullptr;

private:
    void VisualizeGrid();
    FIntVector LastPlayerCell;
    FIntVector GetPlayerCell() const;
    void UpdatePlayerCell();
};
