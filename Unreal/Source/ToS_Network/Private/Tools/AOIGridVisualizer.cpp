#include "Tools/AOIGridVisualizer.h"
#include "DrawDebugHelpers.h"
#include "Engine/World.h"
#include "GameFramework/Actor.h"

AAOIGridVisualizer::AAOIGridVisualizer()
{
    PrimaryActorTick.bCanEverTick = true; 
    LastPlayerCell = FIntVector(-9999, -9999, -9999);
}

void AAOIGridVisualizer::BeginPlay()
{
    Super::BeginPlay();
    VisualizeGrid();
    UpdatePlayerCell();
}

void AAOIGridVisualizer::OnConstruction(const FTransform& Transform)
{
    Super::OnConstruction(Transform);
    VisualizeGrid();
}

void AAOIGridVisualizer::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
    Super::EndPlay(EndPlayReason);

    if (UWorld* World = GetWorld())
    {
        FlushPersistentDebugLines(World);
    }
}

void AAOIGridVisualizer::Destroyed()
{
    Super::Destroyed();
    if (UWorld* World = GetWorld())
    {
        FlushPersistentDebugLines(World);
    }
}

void AAOIGridVisualizer::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);
    if (PlayerActor)
    {
        FIntVector CurrentCell = GetPlayerCell();
        if (CurrentCell != LastPlayerCell)
        {
            LastPlayerCell = CurrentCell;
            VisualizeGrid();
        }
    }
}

void AAOIGridVisualizer::VisualizeGrid()
{
    FVector Origin = GetActorLocation();
    FIntVector PlayerCell = GetPlayerCell();

    for (int X = 0; X < GridWidth; X++)
    {
        for (int Y = 0; Y < GridHeight; Y++)
        {
            for (int Z = 0; Z < GridDepth; Z++)
            {
                FVector CellCenter = Origin + FVector(
                    X * CellSize.X + CellSize.X / 2,
                    Y * CellSize.Y + CellSize.Y / 2,
                    Z * CellSize.Z + CellSize.Z / 2);

                FColor DrawColor = GridColor;
                FIntVector CellCoord(X, Y, Z);

                if (PlayerCell.X >= 0 && PlayerCell.Y >= 0 && PlayerCell.Z >= 0 &&
                    FMath::Abs(X - PlayerCell.X) <= 2 &&
                    FMath::Abs(Y - PlayerCell.Y) <= 2 &&
                    FMath::Abs(Z - PlayerCell.Z) <= 2)
                {
                    DrawColor = InterestAreaColor;
                }
                else if (ActiveCells.Contains(CellCoord))
                {
                    DrawColor = ActiveCellColor;
                }

                DrawDebugBox(GetWorld(), CellCenter, CellSize / 2, DrawColor, true, -1.0f, 0, 2.0f);
            }
        }
    }
}

FIntVector AAOIGridVisualizer::GetPlayerCell() const
{
    if (!PlayerActor) return FIntVector(-1, -1, -1);
    FVector Origin = GetActorLocation();
    FVector PlayerLocal = PlayerActor->GetActorLocation() - Origin;
    return FIntVector(
        FMath::FloorToInt(PlayerLocal.X / CellSize.X),
        FMath::FloorToInt(PlayerLocal.Y / CellSize.Y),
        FMath::FloorToInt(PlayerLocal.Z / CellSize.Z));
}

void AAOIGridVisualizer::UpdatePlayerCell()
{
    LastPlayerCell = GetPlayerCell();
}
