#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"

struct RemoveEntityPacket
{
    uint32 EntityId;

    int32 GetSize() const { return 7; }

    void Serialize(UFlatBuffer* Buffer) const;
    void Deserialize(UFlatBuffer* Buffer);
};

inline void RemoveEntityPacket::Serialize(UFlatBuffer* Buffer) const
{
    Buffer->WriteByte(static_cast<uint8>(EPacketType::Reliable));
    Buffer->WriteUInt16(static_cast<uint16>(ServerPacket::RemoveEntity));
    Buffer->WriteUInt32(EntityId);
}

inline void RemoveEntityPacket::Deserialize(UFlatBuffer* Buffer)
{
    EntityId = Buffer->ReadUInt32();
}
