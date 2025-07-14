#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"

struct ConnectionAcceptedPacket
{
    uint32 Id;

    int32 GetSize() const { return 5; }

    void Serialize(UFlatBuffer* Buffer) const;
    void Deserialize(UFlatBuffer* Buffer);
};

inline void ConnectionAcceptedPacket::Serialize(UFlatBuffer* Buffer) const
{
    Buffer->WriteByte(static_cast<uint8>(EPacketType::ConnectionAccepted));
    Buffer->WriteUInt32(Id);
}

inline void ConnectionAcceptedPacket::Deserialize(UFlatBuffer* Buffer)
{
    Id = Buffer->ReadUInt32();
}
