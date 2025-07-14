#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"

struct SyncEntityPacket
{
    FVector Positon;
    FRotator Rotator;
    uint16 AnimationState;
    uint32 Flags;

    int32 GetSize() const { return 33; }

    void Serialize(UFlatBuffer* Buffer) const;
    void Deserialize(UFlatBuffer* Buffer);
};

inline void SyncEntityPacket::Serialize(UFlatBuffer* Buffer) const
{
    Buffer->WriteByte(static_cast<uint8>(EPacketType::Unreliable));
    Buffer->WriteUInt16(static_cast<uint16>(ServerPacket::SyncEntity));
    Buffer->Write<FVector>(Positon);
    Buffer->Write<FRotator>(Rotator);
    Buffer->WriteUInt16(AnimationState);
    Buffer->WriteUInt32(Flags);
}

inline void SyncEntityPacket::Deserialize(UFlatBuffer* Buffer)
{
    Positon = Buffer->Read<FVector>();
    Rotator = Buffer->Read<FRotator>();
    AnimationState = Buffer->ReadUInt16();
    Flags = Buffer->ReadUInt32();
}
