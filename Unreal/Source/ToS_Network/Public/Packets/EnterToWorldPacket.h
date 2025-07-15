// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ClientPackets.h"
#include "EnterToWorldPacket.generated.h"

USTRUCT(BlueprintType)
struct FEnterToWorldPacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    int32 CharacterId;


    int32 GetSize() const { return 7; }

    void Serialize(UFlatBuffer* Buffer)
    {
        Buffer->Write<uint8>(static_cast<uint8>(EPacketType::Unreliable));
        Buffer->Write<uint16>(static_cast<uint16>(EClientPackets::EnterToWorld));
        Buffer->Write<uint32>(static_cast<uint32>(CharacterId));
    }

};
