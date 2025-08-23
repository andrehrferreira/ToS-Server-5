// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "Tools/PositionQuantizationTest.h"
#include "NetworkMovementSimulator.generated.h"

USTRUCT(BlueprintType)
struct FMovementSnapshot
{
    GENERATED_BODY()

    UPROPERTY(BlueprintReadOnly)
    float TimeStamp;

    UPROPERTY(BlueprintReadOnly)
    FVector OriginalPosition;

    UPROPERTY(BlueprintReadOnly)
    FVector QuantizedPosition;

    UPROPERTY(BlueprintReadOnly)
    float CumulativeError;

    UPROPERTY(BlueprintReadOnly)
    EQuantizationType QuantizationType;

    FMovementSnapshot()
        : TimeStamp(0.0f)
        , OriginalPosition(FVector::ZeroVector)
        , QuantizedPosition(FVector::ZeroVector)
        , CumulativeError(0.0f)
        , QuantizationType(EQuantizationType::Original)
    {}
};

UCLASS()
class TOS_NETWORK_API ANetworkMovementSimulator : public AActor
{
    GENERATED_BODY()

public:
    ANetworkMovementSimulator();

protected:
    virtual void BeginPlay() override;
    virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

public:
    virtual void Tick(float DeltaTime) override;

    // Simulation Settings
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Simulation")
    bool bEnableSimulation = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Simulation")
    float NetworkUpdateRate = 20.0f; // Updates per second (like typical game servers)

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Simulation")
    float SimulationSpeed = 100.0f; // Units per second

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Simulation")
    float MovementRadius = 500.0f; // Radius of circular movement

    // Quantization Settings
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Quantization")
    EQuantizationType TestQuantizationType = EQuantizationType::Int16;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Quantization")
    float Int32Range = 1000.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Quantization")
    float Int16Range = 100.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Quantization")
    float Int8Range = 10.0f;

    // Visual Settings
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual")
    bool bShowTrajectory = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual")
    bool bShowErrorAccumulation = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual")
    int32 MaxTrajectoryPoints = 100;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual")
    FColor OriginalTrajectoryColor = FColor::Green;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual")
    FColor QuantizedTrajectoryColor = FColor::Red;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual")
    float TrajectoryPointSize = 5.0f;

    // Statistics
    UPROPERTY(BlueprintReadOnly, Category = "Statistics")
    float AverageError = 0.0f;

    UPROPERTY(BlueprintReadOnly, Category = "Statistics")
    float MaxError = 0.0f;

    UPROPERTY(BlueprintReadOnly, Category = "Statistics")
    float TotalError = 0.0f;

    UPROPERTY(BlueprintReadOnly, Category = "Statistics")
    int32 SampleCount = 0;

    // Reset statistics
    UFUNCTION(BlueprintCallable, Category = "Statistics")
    void ResetStatistics();

    // Get current movement snapshot
    UFUNCTION(BlueprintCallable, Category = "Simulation")
    FMovementSnapshot GetCurrentSnapshot() const;

private:
    // Movement simulation
    float SimulationTime;
    float LastNetworkUpdate;
    FVector StartPosition;

    // Trajectory tracking
    TArray<FVector> OriginalTrajectory;
    TArray<FVector> QuantizedTrajectory;
    TArray<FMovementSnapshot> MovementHistory;

    // Quantization functions (reusing from PositionQuantizationTest)
    FVector QuantizePosition(const FVector& Original, EQuantizationType Type);
    FVector QuantizeToFloat16(const FVector& Original);
    FVector QuantizeToInt32(const FVector& Original);
    FVector QuantizeToInt16(const FVector& Original);
    FVector QuantizeToInt8(const FVector& Original);
    float QuantizeFloat16(float Value);

    // Simulation helpers
    FVector CalculateMovementPosition(float Time);
    void UpdateNetworkSimulation();
    void UpdateStatistics(float Error);
    void VisualizeMovement();
    void ClearVisualization();
};
