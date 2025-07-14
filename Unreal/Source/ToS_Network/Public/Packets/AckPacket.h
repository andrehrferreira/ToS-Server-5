#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"

struct AckPacket
{
    int16 Sequence;

    int32 GetSize() const { return 3; }

    void Serialize(UFlatBuffer* Buffer) const;
    void Deserialize(UFlatBuffer* Buffer);
};

inline void AckPacket::Serialize(UFlatBuffer* Buffer) const
{
    Buffer->WriteByte(static_cast<uint8>(EPacketType::Ack));
    Buffer->WriteInt16(Sequence);
}

inline void AckPacket::Deserialize(UFlatBuffer* Buffer)
{
    Sequence = Buffer->ReadInt16();
}
