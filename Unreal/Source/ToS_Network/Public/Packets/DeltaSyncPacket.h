#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"

struct DeltaSyncPacket
{
    uint32 Index;
    uint8 EntitiesMask;
    int32 EntitiesData;

    int32 GetSize() const { return 8; }

    void Serialize(UFlatBuffer* Buffer) const;
    void Deserialize(UFlatBuffer* Buffer);
};

inline void DeltaSyncPacket::Serialize(UFlatBuffer* Buffer) const
{
    Buffer->WriteByte(static_cast<uint8>(EPacketType::Unreliable));
    Buffer->WriteUInt16(static_cast<uint16>(ServerPacket::DeltaSync));
    Buffer->WriteUInt32(Index);
    Buffer->WriteByte(EntitiesMask);
    // Unsupported type: IntPtr
}

inline void DeltaSyncPacket::Deserialize(UFlatBuffer* Buffer)
{
    Index = Buffer->ReadUInt32();
    EntitiesMask = Buffer->ReadByte();
    // Unsupported type: IntPtr
}
