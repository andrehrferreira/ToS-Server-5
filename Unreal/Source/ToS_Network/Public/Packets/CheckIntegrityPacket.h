#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"

struct CheckIntegrityPacket
{
    uint16 Index;
    uint32 Version;

    int32 GetSize() const { return 7; }

    void Serialize(UFlatBuffer* Buffer) const;
    void Deserialize(UFlatBuffer* Buffer);
};

inline void CheckIntegrityPacket::Serialize(UFlatBuffer* Buffer) const
{
    Buffer->WriteByte(static_cast<uint8>(EPacketType::CheckIntegrity));
    Buffer->WriteUInt16(Index);
    Buffer->WriteUInt32(Version);
}

inline void CheckIntegrityPacket::Deserialize(UFlatBuffer* Buffer)
{
    Index = Buffer->ReadUInt16();
    Version = Buffer->ReadUInt32();
}
