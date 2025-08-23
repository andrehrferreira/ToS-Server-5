#include "Tools/PositionQuantizationTest.h"
#include "DrawDebugHelpers.h"
#include "Engine/World.h"
#include "GameFramework/Actor.h"
#include "Engine/Canvas.h"
#include "Engine/Engine.h"

APositionQuantizationTest::APositionQuantizationTest()
{
    PrimaryActorTick.bCanEverTick = true;
    LastTrackedPosition = FVector::ZeroVector;
    TimeSinceLastUpdate = 0.0f;
}

void APositionQuantizationTest::BeginPlay()
{
    Super::BeginPlay();
    if (TrackedActor)
    {
        LastTrackedPosition = TrackedActor->GetActorLocation();
        VisualizeQuantization();
    }
}

void APositionQuantizationTest::OnConstruction(const FTransform& Transform)
{
    Super::OnConstruction(Transform);
    if (TrackedActor)
    {
        VisualizeQuantization();
    }
}

void APositionQuantizationTest::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
    Super::EndPlay(EndPlayReason);
    ClearDebugDrawing();
}

void APositionQuantizationTest::Destroyed()
{
    Super::Destroyed();
    ClearDebugDrawing();
}

void APositionQuantizationTest::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);

    TimeSinceLastUpdate += DeltaTime;

    if (TrackedActor && TimeSinceLastUpdate >= UpdateRate)
    {
        FVector CurrentPosition = TrackedActor->GetActorLocation();
        if (!CurrentPosition.Equals(LastTrackedPosition, 0.1f))
        {
            LastTrackedPosition = CurrentPosition;
            VisualizeQuantization();
        }
        TimeSinceLastUpdate = 0.0f;
    }
}

float APositionQuantizationTest::QuantizeFloat16(float Value)
{
    // Simulate float16 precision
    // Float16 has 10 bits of mantissa, so we lose precision
    // This is a simplified approximation
    const float Scale = 1024.0f; // 2^10
    return FMath::RoundToFloat(Value * Scale) / Scale;
}

FVector APositionQuantizationTest::QuantizeToFloat16(const FVector& Original)
{
    return FVector(
        QuantizeFloat16(Original.X),
        QuantizeFloat16(Original.Y),
        QuantizeFloat16(Original.Z)
    );
}

FVector APositionQuantizationTest::QuantizeToInt32(const FVector& Original)
{
    // Int32 can represent values from -2,147,483,648 to 2,147,483,647
    // We scale based on the range setting
    const float Scale = 2147483647.0f / Int32Range;

    return FVector(
        FMath::RoundToFloat(FMath::Clamp(Original.X, -Int32Range, Int32Range) * Scale) / Scale,
        FMath::RoundToFloat(FMath::Clamp(Original.Y, -Int32Range, Int32Range) * Scale) / Scale,
        FMath::RoundToFloat(FMath::Clamp(Original.Z, -Int32Range, Int32Range) * Scale) / Scale
    );
}

FVector APositionQuantizationTest::QuantizeToInt16(const FVector& Original)
{
    // Int16 can represent values from -32,768 to 32,767
    const float Scale = 32767.0f / Int16Range;

    return FVector(
        FMath::RoundToFloat(FMath::Clamp(Original.X, -Int16Range, Int16Range) * Scale) / Scale,
        FMath::RoundToFloat(FMath::Clamp(Original.Y, -Int16Range, Int16Range) * Scale) / Scale,
        FMath::RoundToFloat(FMath::Clamp(Original.Z, -Int16Range, Int16Range) * Scale) / Scale
    );
}

FVector APositionQuantizationTest::QuantizeToInt8(const FVector& Original)
{
    // Int8 can represent values from -128 to 127
    const float Scale = 127.0f / Int8Range;

    return FVector(
        FMath::RoundToFloat(FMath::Clamp(Original.X, -Int8Range, Int8Range) * Scale) / Scale,
        FMath::RoundToFloat(FMath::Clamp(Original.Y, -Int8Range, Int8Range) * Scale) / Scale,
        FMath::RoundToFloat(FMath::Clamp(Original.Z, -Int8Range, Int8Range) * Scale) / Scale
    );
}

float APositionQuantizationTest::CalculateError(const FVector& Original, const FVector& Quantized)
{
    return FVector::Dist(Original, Quantized);
}

void APositionQuantizationTest::VisualizeQuantization()
{
    if (!TrackedActor || !GetWorld())
    {
        return;
    }

    ClearDebugDrawing();
    CurrentResults.Empty();

    FVector OriginalPosition = TrackedActor->GetActorLocation();

    // Calculate all quantized positions
    FQuantizationResult OriginalResult;
    OriginalResult.OriginalPosition = OriginalPosition;
    OriginalResult.QuantizedPosition = OriginalPosition;
    OriginalResult.Error = 0.0f;
    OriginalResult.Type = EQuantizationType::Original;
    CurrentResults.Add(OriginalResult);

    if (bShowFloat16)
    {
        FVector QuantizedPos = QuantizeToFloat16(OriginalPosition);
        FQuantizationResult Result;
        Result.OriginalPosition = OriginalPosition;
        Result.QuantizedPosition = QuantizedPos;
        Result.Error = CalculateError(OriginalPosition, QuantizedPos);
        Result.Type = EQuantizationType::Float16;
        CurrentResults.Add(Result);
    }

    if (bShowInt32)
    {
        FVector QuantizedPos = QuantizeToInt32(OriginalPosition);
        FQuantizationResult Result;
        Result.OriginalPosition = OriginalPosition;
        Result.QuantizedPosition = QuantizedPos;
        Result.Error = CalculateError(OriginalPosition, QuantizedPos);
        Result.Type = EQuantizationType::Int32;
        CurrentResults.Add(Result);
    }

    if (bShowInt16)
    {
        FVector QuantizedPos = QuantizeToInt16(OriginalPosition);
        FQuantizationResult Result;
        Result.OriginalPosition = OriginalPosition;
        Result.QuantizedPosition = QuantizedPos;
        Result.Error = CalculateError(OriginalPosition, QuantizedPos);
        Result.Type = EQuantizationType::Int16;
        CurrentResults.Add(Result);
    }

    if (bShowInt8)
    {
        FVector QuantizedPos = QuantizeToInt8(OriginalPosition);
        FQuantizationResult Result;
        Result.OriginalPosition = OriginalPosition;
        Result.QuantizedPosition = QuantizedPos;
        Result.Error = CalculateError(OriginalPosition, QuantizedPos);
        Result.Type = EQuantizationType::Int8;
        CurrentResults.Add(Result);
    }

    // Draw visual representations
    for (const FQuantizationResult& Result : CurrentResults)
    {
        FColor DrawColor;
        FString TypeName;

        switch (Result.Type)
        {
            case EQuantizationType::Original:
                if (!bShowOriginalPosition) continue;
                DrawColor = OriginalColor;
                TypeName = TEXT("Original");
                break;
            case EQuantizationType::Float16:
                DrawColor = Float16Color;
                TypeName = TEXT("Float16");
                break;
            case EQuantizationType::Int32:
                DrawColor = Int32Color;
                TypeName = TEXT("Int32");
                break;
            case EQuantizationType::Int16:
                DrawColor = Int16Color;
                TypeName = TEXT("Int16");
                break;
            case EQuantizationType::Int8:
                DrawColor = Int8Color;
                TypeName = TEXT("Int8");
                break;
        }

        // Draw sphere at quantized position
        DrawDebugSphere(GetWorld(), Result.QuantizedPosition, SphereRadius, 12, DrawColor, true, -1.0f, 0, 2.0f);

        // Draw error line if enabled and there's an error
        if (bShowErrorLines && Result.Error > 0.001f)
        {
            DrawDebugLine(GetWorld(), OriginalPosition, Result.QuantizedPosition, FColor::Orange, true, -1.0f, 0, 1.0f);
        }

        // Draw error text if enabled
        if (bShowErrorText)
        {
            FString ErrorText = FString::Printf(TEXT("%s\nError: %.3f cm\nPos: %.1f, %.1f, %.1f"),
                *TypeName,
                Result.Error,
                Result.QuantizedPosition.X,
                Result.QuantizedPosition.Y,
                Result.QuantizedPosition.Z);

            FVector TextLocation = Result.QuantizedPosition + FVector(0, 0, SphereRadius + 50.0f);
            DrawDebugString(GetWorld(), TextLocation, ErrorText, nullptr, DrawColor, -1.0f, true, 1.2f);
        }
    }

    // Draw summary information
    if (bShowErrorText && CurrentResults.Num() > 1)
    {
        FString SummaryText = TEXT("Quantization Errors:\n");
        for (const FQuantizationResult& Result : CurrentResults)
        {
            if (Result.Type == EQuantizationType::Original) continue;

            FString TypeName;
            switch (Result.Type)
            {
                case EQuantizationType::Float16: TypeName = TEXT("Float16"); break;
                case EQuantizationType::Int32: TypeName = TEXT("Int32"); break;
                case EQuantizationType::Int16: TypeName = TEXT("Int16"); break;
                case EQuantizationType::Int8: TypeName = TEXT("Int8"); break;
                default: TypeName = TEXT("Unknown"); break;
            }

            SummaryText += FString::Printf(TEXT("%s: %.3f cm\n"), *TypeName, Result.Error);
        }

        FVector SummaryLocation = OriginalPosition + FVector(200, 0, 200);
        DrawDebugString(GetWorld(), SummaryLocation, SummaryText, nullptr, FColor::White, -1.0f, true, 1.0f);
    }
}

void APositionQuantizationTest::ClearDebugDrawing()
{
    if (UWorld* World = GetWorld())
    {
        FlushPersistentDebugLines(World);
    }
}
