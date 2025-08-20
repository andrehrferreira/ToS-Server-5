// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ClientPackets.h"
#include "RekeyResponsePacket.generated.h"

USTRUCT(BlueprintType)
struct FRekeyResponsePacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    bool Accepted;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int64 AcknowledgedSequence;


    int32 GetSize() const { return 12; }

    void Serialize(UFlatBuffer* Buffer)
    {
        Buffer->Write<uint8>(static_cast<uint8>(EPacketType::Reliable));
        Buffer->Write<uint16>(static_cast<uint16>(EClientPackets::RekeyResponse));
        Buffer->Write<bool>(Accepted);
        Buffer->Write<int64>(static_cast<int64>(AcknowledgedSequence));
    }

};
