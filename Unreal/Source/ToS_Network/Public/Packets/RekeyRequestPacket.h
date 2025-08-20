// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "RekeyRequestPacket.generated.h"

USTRUCT(BlueprintType)
struct FRekeyRequestPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int64 CurrentSequence;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    TArray<uint8> NewSalt;


    int32 GetSize() const { return 27; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        CurrentSequence = Buffer->Read<int64>();
        NewSalt.SetNumUninitialized(16);
        Buffer->ReadBytes(NewSalt.GetData(), 16);
    }
};
