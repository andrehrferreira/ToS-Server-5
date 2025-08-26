// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ClientPackets.h"
#include "SyncEntityPacket.generated.h"

USTRUCT(BlueprintType)
struct FSyncEntityPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FVector Positon;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FRotator Rotator;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FVector Velocity;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 AnimationState;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    bool IsFalling;


    int32 GetSize() const { return 24; }

    void Serialize(UFlatBuffer* Buffer)
    {
        Buffer->Write<uint8>(static_cast<uint8>(EPacketType::Unreliable));
        Buffer->Write<uint16>(static_cast<uint16>(EClientPackets::SyncEntity));
        Buffer->Write<FVector>(Positon);
        Buffer->Write<FRotator>(Rotator);
        Buffer->Write<FVector>(Velocity);
        Buffer->Write<uint16>(static_cast<uint16>(AnimationState));
        Buffer->Write<bool>(IsFalling);
    }

};
