#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"

struct SyncStateIntPacket
{

    int32 GetSize() const { return 3; }

    void Serialize(UFlatBuffer* Buffer) const;
    void Deserialize(UFlatBuffer* Buffer);
};

inline void SyncStateIntPacket::Serialize(UFlatBuffer* Buffer) const
{
    Buffer->WriteByte(static_cast<uint8>(EPacketType::Reliable));
    Buffer->WriteUInt16(static_cast<uint16>(ServerPacket::SyncStateInt));
}

inline void SyncStateIntPacket::Deserialize(UFlatBuffer* Buffer)
{
}
