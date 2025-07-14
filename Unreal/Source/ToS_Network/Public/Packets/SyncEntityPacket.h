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
    int32 AnimationState;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 Flags;


    int32 GetSize() const { return 33; }

    void Serialize(UFlatBuffer* Buffer)
    {
        Buffer->WriteByte(static_cast<uint8>(EPacketType::Unreliable));
        Buffer->WriteUInt16(static_cast<uint16>(EClientPackets::SyncEntity));
        Buffer->Write<FVector>(Positon);
        Buffer->Write<FRotator>(Rotator);
        Buffer->WriteUInt16(static_cast<uint16>(AnimationState));
        Buffer->WriteUInt32(static_cast<uint32>(Flags));
    }

};
