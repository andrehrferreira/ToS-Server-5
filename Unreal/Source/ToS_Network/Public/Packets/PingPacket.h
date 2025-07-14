#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"

struct PingPacket
{
    uint16 SentTimestamp;

    int32 GetSize() const { return 3; }

    void Serialize(UFlatBuffer* Buffer) const;
    void Deserialize(UFlatBuffer* Buffer);
};

inline void PingPacket::Serialize(UFlatBuffer* Buffer) const
{
    Buffer->WriteByte(static_cast<uint8>(EPacketType::Ping));
    Buffer->WriteUInt16(SentTimestamp);
}

inline void PingPacket::Deserialize(UFlatBuffer* Buffer)
{
    SentTimestamp = Buffer->ReadUInt16();
}
