#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"

struct CreateEntityPacket
{
    uint32 EntityId;
    FVector Positon;
    FRotator Rotator;
    uint32 Flags;

    int32 GetSize() const { return 35; }

    void Serialize(UFlatBuffer* Buffer) const;
    void Deserialize(UFlatBuffer* Buffer);
};

inline void CreateEntityPacket::Serialize(UFlatBuffer* Buffer) const
{
    Buffer->WriteByte(static_cast<uint8>(EPacketType::Reliable));
    Buffer->WriteUInt16(static_cast<uint16>(ServerPacket::CreateEntity));
    Buffer->WriteUInt32(EntityId);
    Buffer->Write<FVector>(Positon);
    Buffer->Write<FRotator>(Rotator);
    Buffer->WriteUInt32(Flags);
}

inline void CreateEntityPacket::Deserialize(UFlatBuffer* Buffer)
{
    EntityId = Buffer->ReadUInt32();
    Positon = Buffer->Read<FVector>();
    Rotator = Buffer->Read<FRotator>();
    Flags = Buffer->ReadUInt32();
}
