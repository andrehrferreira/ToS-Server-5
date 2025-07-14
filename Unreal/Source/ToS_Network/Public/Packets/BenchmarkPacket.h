#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"

struct BenchmarkPacket
{
    uint32 Id;
    FVector Positon;
    FRotator Rotator;

    int32 GetSize() const { return 31; }

    void Serialize(UFlatBuffer* Buffer) const;
    void Deserialize(UFlatBuffer* Buffer);
};

inline void BenchmarkPacket::Serialize(UFlatBuffer* Buffer) const
{
    Buffer->WriteByte(static_cast<uint8>(EPacketType::Unreliable));
    Buffer->WriteUInt16(static_cast<uint16>(ServerPacket::Benchmark));
    Buffer->WriteUInt32(Id);
    Buffer->Write<FVector>(Positon);
    Buffer->Write<FRotator>(Rotator);
}

inline void BenchmarkPacket::Deserialize(UFlatBuffer* Buffer)
{
    Id = Buffer->ReadUInt32();
    Positon = Buffer->Read<FVector>();
    Rotator = Buffer->Read<FRotator>();
}
