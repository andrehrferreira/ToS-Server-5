// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "BenchmarkPacket.generated.h"

USTRUCT(BlueprintType)
struct FBenchmarkPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Id;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FVector Positon;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FRotator Rotator;


    int32 GetSize() const { return 31; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        Id = static_cast<int32>(Buffer->Read<uint32>());
        Positon = Buffer->Read<FVector>();
        Rotator = Buffer->Read<FRotator>();
    }
};
