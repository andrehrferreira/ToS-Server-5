#include "Tools/NetworkMovementSimulator.h"
#include "DrawDebugHelpers.h"
#include "Engine/World.h"
#include "Engine/Engine.h"

ANetworkMovementSimulator::ANetworkMovementSimulator()
{
    PrimaryActorTick.bCanEverTick = true;
    SimulationTime = 0.0f;
    LastNetworkUpdate = 0.0f;
    StartPosition = FVector::ZeroVector;
}

void ANetworkMovementSimulator::BeginPlay()
{
    Super::BeginPlay();
    StartPosition = GetActorLocation();
    ResetStatistics();
}

void ANetworkMovementSimulator::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
    Super::EndPlay(EndPlayReason);
    ClearVisualization();
}

void ANetworkMovementSimulator::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);

    if (!bEnableSimulation)
        return;

    SimulationTime += DeltaTime;

    // Update network simulation at specified rate
    float TimeSinceLastUpdate = SimulationTime - LastNetworkUpdate;
    if (TimeSinceLastUpdate >= (1.0f / NetworkUpdateRate))
    {
        UpdateNetworkSimulation();
        LastNetworkUpdate = SimulationTime;
    }

    VisualizeMovement();
}

void ANetworkMovementSimulator::ResetStatistics()
{
    AverageError = 0.0f;
    MaxError = 0.0f;
    TotalError = 0.0f;
    SampleCount = 0;
    OriginalTrajectory.Empty();
    QuantizedTrajectory.Empty();
    MovementHistory.Empty();
    SimulationTime = 0.0f;
    LastNetworkUpdate = 0.0f;
}

FMovementSnapshot ANetworkMovementSimulator::GetCurrentSnapshot() const
{
    if (MovementHistory.Num() > 0)
    {
        return MovementHistory.Last();
    }
    return FMovementSnapshot();
}

float ANetworkMovementSimulator::QuantizeFloat16(float Value)
{
    // Simulate float16 precision (10 bits mantissa)
    const float Scale = 1024.0f; // 2^10
    return FMath::RoundToFloat(Value * Scale) / Scale;
}

FVector ANetworkMovementSimulator::QuantizeToFloat16(const FVector& Original)
{
    return FVector(
        QuantizeFloat16(Original.X),
        QuantizeFloat16(Original.Y),
        QuantizeFloat16(Original.Z)
    );
}

FVector ANetworkMovementSimulator::QuantizeToInt32(const FVector& Original)
{
    const float Scale = 2147483647.0f / Int32Range;
    return FVector(
        FMath::RoundToFloat(FMath::Clamp(Original.X, -Int32Range, Int32Range) * Scale) / Scale,
        FMath::RoundToFloat(FMath::Clamp(Original.Y, -Int32Range, Int32Range) * Scale) / Scale,
        FMath::RoundToFloat(FMath::Clamp(Original.Z, -Int32Range, Int32Range) * Scale) / Scale
    );
}

FVector ANetworkMovementSimulator::QuantizeToInt16(const FVector& Original)
{
    const float Scale = 32767.0f / Int16Range;
    return FVector(
        FMath::RoundToFloat(FMath::Clamp(Original.X, -Int16Range, Int16Range) * Scale) / Scale,
        FMath::RoundToFloat(FMath::Clamp(Original.Y, -Int16Range, Int16Range) * Scale) / Scale,
        FMath::RoundToFloat(FMath::Clamp(Original.Z, -Int16Range, Int16Range) * Scale) / Scale
    );
}

FVector ANetworkMovementSimulator::QuantizeToInt8(const FVector& Original)
{
    const float Scale = 127.0f / Int8Range;
    return FVector(
        FMath::RoundToFloat(FMath::Clamp(Original.X, -Int8Range, Int8Range) * Scale) / Scale,
        FMath::RoundToFloat(FMath::Clamp(Original.Y, -Int8Range, Int8Range) * Scale) / Scale,
        FMath::RoundToFloat(FMath::Clamp(Original.Z, -Int8Range, Int8Range) * Scale) / Scale
    );
}

FVector ANetworkMovementSimulator::QuantizePosition(const FVector& Original, EQuantizationType Type)
{
    switch (Type)
    {
        case EQuantizationType::Float16:
            return QuantizeToFloat16(Original);
        case EQuantizationType::Int32:
            return QuantizeToInt32(Original);
        case EQuantizationType::Int16:
            return QuantizeToInt16(Original);
        case EQuantizationType::Int8:
            return QuantizeToInt8(Original);
        default:
            return Original;
    }
}

FVector ANetworkMovementSimulator::CalculateMovementPosition(float Time)
{
    // Circular movement pattern
    float Angle = (Time * SimulationSpeed) / MovementRadius;
    FVector Offset = FVector(
        FMath::Cos(Angle) * MovementRadius,
        FMath::Sin(Angle) * MovementRadius,
        FMath::Sin(Angle * 0.5f) * (MovementRadius * 0.2f) // Add some Z movement
    );
    return StartPosition + Offset;
}

void ANetworkMovementSimulator::UpdateNetworkSimulation()
{
    // Calculate the true position
    FVector TruePosition = CalculateMovementPosition(SimulationTime);

    // Quantize the position based on selected type
    FVector QuantizedPosition = QuantizePosition(TruePosition, TestQuantizationType);

    // Calculate error
    float Error = FVector::Dist(TruePosition, QuantizedPosition);

    // Add to trajectories
    OriginalTrajectory.Add(TruePosition);
    QuantizedTrajectory.Add(QuantizedPosition);

    // Limit trajectory length
    if (OriginalTrajectory.Num() > MaxTrajectoryPoints)
    {
        OriginalTrajectory.RemoveAt(0);
        QuantizedTrajectory.RemoveAt(0);
    }

    // Create movement snapshot
    FMovementSnapshot Snapshot;
    Snapshot.TimeStamp = SimulationTime;
    Snapshot.OriginalPosition = TruePosition;
    Snapshot.QuantizedPosition = QuantizedPosition;
    Snapshot.CumulativeError = TotalError + Error;
    Snapshot.QuantizationType = TestQuantizationType;

    MovementHistory.Add(Snapshot);
    if (MovementHistory.Num() > MaxTrajectoryPoints)
    {
        MovementHistory.RemoveAt(0);
    }

    // Update statistics
    UpdateStatistics(Error);
}

void ANetworkMovementSimulator::UpdateStatistics(float Error)
{
    SampleCount++;
    TotalError += Error;
    AverageError = TotalError / SampleCount;

    if (Error > MaxError)
    {
        MaxError = Error;
    }
}

void ANetworkMovementSimulator::VisualizeMovement()
{
    if (!GetWorld())
        return;

    // Clear previous visualization
    ClearVisualization();

    // Draw trajectory if enabled
    if (bShowTrajectory)
    {
        // Draw original trajectory
        for (int32 i = 1; i < OriginalTrajectory.Num(); i++)
        {
            DrawDebugLine(GetWorld(), OriginalTrajectory[i-1], OriginalTrajectory[i],
                         OriginalTrajectoryColor, false, -1.0f, 0, 2.0f);

            // Draw point
            DrawDebugSphere(GetWorld(), OriginalTrajectory[i], TrajectoryPointSize, 8,
                           OriginalTrajectoryColor, false, -1.0f, 0, 1.0f);
        }

        // Draw quantized trajectory
        for (int32 i = 1; i < QuantizedTrajectory.Num(); i++)
        {
            DrawDebugLine(GetWorld(), QuantizedTrajectory[i-1], QuantizedTrajectory[i],
                         QuantizedTrajectoryColor, false, -1.0f, 0, 2.0f);

            // Draw point
            DrawDebugSphere(GetWorld(), QuantizedTrajectory[i], TrajectoryPointSize, 8,
                           QuantizedTrajectoryColor, false, -1.0f, 0, 1.0f);
        }

        // Draw error lines if enabled
        if (bShowErrorAccumulation && OriginalTrajectory.Num() == QuantizedTrajectory.Num())
        {
            for (int32 i = 0; i < OriginalTrajectory.Num(); i++)
            {
                float Error = FVector::Dist(OriginalTrajectory[i], QuantizedTrajectory[i]);
                if (Error > 0.001f)
                {
                    FColor ErrorColor = FColor::Orange;
                    ErrorColor.A = FMath::Clamp((uint8)(Error * 10), (uint8)50, (uint8)255);
                    DrawDebugLine(GetWorld(), OriginalTrajectory[i], QuantizedTrajectory[i],
                                 ErrorColor, false, -1.0f, 0, 1.0f);
                }
            }
        }
    }

    // Draw current position markers
    if (OriginalTrajectory.Num() > 0)
    {
        FVector CurrentOriginal = OriginalTrajectory.Last();
        FVector CurrentQuantized = QuantizedTrajectory.Last();

        // Larger spheres for current positions
        DrawDebugSphere(GetWorld(), CurrentOriginal, TrajectoryPointSize * 2, 12,
                       OriginalTrajectoryColor, false, -1.0f, 0, 3.0f);
        DrawDebugSphere(GetWorld(), CurrentQuantized, TrajectoryPointSize * 2, 12,
                       QuantizedTrajectoryColor, false, -1.0f, 0, 3.0f);
    }

    // Draw statistics text
    FVector StatsLocation = StartPosition + FVector(0, 0, 300);

    FString QuantizationName;
    switch (TestQuantizationType)
    {
        case EQuantizationType::Float16: QuantizationName = TEXT("Float16"); break;
        case EQuantizationType::Int32: QuantizationName = TEXT("Int32"); break;
        case EQuantizationType::Int16: QuantizationName = TEXT("Int16"); break;
        case EQuantizationType::Int8: QuantizationName = TEXT("Int8"); break;
        default: QuantizationName = TEXT("Original"); break;
    }

    FString StatsText = FString::Printf(
        TEXT("Movement Simulation (%s)\n")
        TEXT("Network Rate: %.1f Hz\n")
        TEXT("Samples: %d\n")
        TEXT("Avg Error: %.3f cm\n")
        TEXT("Max Error: %.3f cm\n")
        TEXT("Total Error: %.1f cm"),
        *QuantizationName,
        NetworkUpdateRate,
        SampleCount,
        AverageError,
        MaxError,
        TotalError
    );

    DrawDebugString(GetWorld(), StatsLocation, StatsText, nullptr, FColor::White,
                   0.0f, true, 1.2f);
}

void ANetworkMovementSimulator::ClearVisualization()
{
    if (UWorld* World = GetWorld())
    {
        FlushDebugStrings(World);
    }
}
