// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ClientPackets.h"
#include "SyncEntityQuantizedPacket.generated.h"

USTRUCT(BlueprintType)
struct FSyncEntityQuantizedPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 QuantizedX;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 QuantizedY;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 QuantizedZ;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 QuadrantX;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 QuadrantY;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    float Yaw;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    FVector Velocity;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 AnimationState;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    bool IsFalling;


    int32 GetSize() const { return 26; }

    void Serialize(UFlatBuffer* Buffer)
    {
        Buffer->Write<uint8>(static_cast<uint8>(EPacketType::Unreliable));
        Buffer->Write<uint16>(static_cast<uint16>(EClientPackets::SyncEntityQuantized));
        Buffer->Write<int16>(static_cast<int16>(QuantizedX));
        Buffer->Write<int16>(static_cast<int16>(QuantizedY));
        Buffer->Write<int16>(static_cast<int16>(QuantizedZ));
        Buffer->Write<int16>(static_cast<int16>(QuadrantX));
        Buffer->Write<int16>(static_cast<int16>(QuadrantY));
        Buffer->Write<float>(Yaw);
        Buffer->Write<FVector>(Velocity);
        Buffer->Write<uint16>(static_cast<uint16>(AnimationState));
        Buffer->Write<bool>(IsFalling);
    }

};
