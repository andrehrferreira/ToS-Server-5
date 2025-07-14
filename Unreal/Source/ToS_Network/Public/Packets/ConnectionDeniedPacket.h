#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"

struct ConnectionDeniedPacket
{

    int32 GetSize() const { return 1; }

    void Serialize(UFlatBuffer* Buffer) const;
    void Deserialize(UFlatBuffer* Buffer);
};

inline void ConnectionDeniedPacket::Serialize(UFlatBuffer* Buffer) const
{
    Buffer->WriteByte(static_cast<uint8>(EPacketType::ConnectionDenied));
}

inline void ConnectionDeniedPacket::Deserialize(UFlatBuffer* Buffer)
{
}
