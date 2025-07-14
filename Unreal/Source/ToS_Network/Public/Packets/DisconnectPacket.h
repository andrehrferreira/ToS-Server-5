#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"

struct DisconnectPacket
{

    int32 GetSize() const { return 1; }

    void Serialize(UFlatBuffer* Buffer) const;
    void Deserialize(UFlatBuffer* Buffer);
};

inline void DisconnectPacket::Serialize(UFlatBuffer* Buffer) const
{
    Buffer->WriteByte(static_cast<uint8>(EPacketType::Disconnect));
}

inline void DisconnectPacket::Deserialize(UFlatBuffer* Buffer)
{
}
