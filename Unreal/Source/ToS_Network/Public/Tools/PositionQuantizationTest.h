// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "PositionQuantizationTest.generated.h"

UENUM(BlueprintType)
enum class EQuantizationType : uint8
{
    Original    UMETA(DisplayName = "Original Float"),
    Float16     UMETA(DisplayName = "Float16"),
    Int32       UMETA(DisplayName = "Int32"),
    Int16       UMETA(DisplayName = "Int16"),
    Int8        UMETA(DisplayName = "Int8")
};

USTRUCT(BlueprintType)
struct FQuantizationResult
{
    GENERATED_BODY()

    UPROPERTY(BlueprintReadOnly)
    FVector OriginalPosition;

    UPROPERTY(BlueprintReadOnly)
    FVector QuantizedPosition;

    UPROPERTY(BlueprintReadOnly)
    float Error;

    UPROPERTY(BlueprintReadOnly)
    EQuantizationType Type;

    FQuantizationResult()
        : OriginalPosition(FVector::ZeroVector)
        , QuantizedPosition(FVector::ZeroVector)
        , Error(0.0f)
        , Type(EQuantizationType::Original)
    {}
};

UCLASS()
class TOS_NETWORK_API APositionQuantizationTest : public AActor
{
    GENERATED_BODY()

public:
    APositionQuantizationTest();

protected:
    virtual void BeginPlay() override;
    virtual void OnConstruction(const FTransform& Transform) override;
    virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;
    virtual void Destroyed() override;

public:
    virtual void Tick(float DeltaTime) override;

    // Configuration Properties
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Test Settings")
    AActor* TrackedActor = nullptr;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Test Settings")
    bool bShowOriginalPosition = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Test Settings")
    bool bShowFloat16 = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Test Settings")
    bool bShowInt32 = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Test Settings")
    bool bShowInt16 = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Test Settings")
    bool bShowInt8 = true;

    // Quantization ranges for integer types
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Quantization Settings", meta = (ClampMin = "1.0", ClampMax = "10000.0"))
    float Int32Range = 1000.0f; // ±1000 units

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Quantization Settings", meta = (ClampMin = "1.0", ClampMax = "1000.0"))
    float Int16Range = 100.0f; // ±100 units

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Quantization Settings", meta = (ClampMin = "1.0", ClampMax = "100.0"))
    float Int8Range = 10.0f; // ±10 units

    // Visual properties
    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual Settings")
    FColor OriginalColor = FColor::White;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual Settings")
    FColor Float16Color = FColor::Green;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual Settings")
    FColor Int32Color = FColor::Blue;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual Settings")
    FColor Int16Color = FColor::Yellow;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual Settings")
    FColor Int8Color = FColor::Red;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual Settings")
    float SphereRadius = 10.0f;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual Settings")
    bool bShowErrorLines = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual Settings")
    bool bShowErrorText = true;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Visual Settings")
    float UpdateRate = 0.1f; // Update every 0.1 seconds

private:
    // Quantization functions
    FVector QuantizeToFloat16(const FVector& Original);
    FVector QuantizeToInt32(const FVector& Original);
    FVector QuantizeToInt16(const FVector& Original);
    FVector QuantizeToInt8(const FVector& Original);

    // Utility functions
    float QuantizeFloat16(float Value);
    float CalculateError(const FVector& Original, const FVector& Quantized);
    void VisualizeQuantization();
    void ClearDebugDrawing();

    // State tracking
    FVector LastTrackedPosition;
    float TimeSinceLastUpdate;
    TArray<FQuantizationResult> CurrentResults;
};
