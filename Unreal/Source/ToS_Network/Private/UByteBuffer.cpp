#include "UByteBuffer.h"

UByteBuffer* UByteBuffer::CreateEmptyByteBuffer(int32 Capacity)
{
    UByteBuffer* ByteBuffer = NewObject<UByteBuffer>();
    ByteBuffer->Buffer.SetNumUninitialized(Capacity); 
    ByteBuffer->Offset = 0;
    ByteBuffer->Length = 0;
    return ByteBuffer;
}

UByteBuffer* UByteBuffer::CreateByteBuffer(const TArray<uint8>& Data)
{
    UByteBuffer* ByteBuffer = NewObject<UByteBuffer>();
    int32 DataSize = Data.Num();
    ByteBuffer->Buffer.SetNumUninitialized(DataSize);

    if (DataSize > 0)    
        FMemory::Memcpy(ByteBuffer->Buffer.GetData(), Data.GetData(), DataSize);
    
    ByteBuffer->Offset = 0;
    ByteBuffer->Length = DataSize;
    return ByteBuffer;
}

//Write 

UByteBuffer* UByteBuffer::WriteByte(uint8 Value)
{
    if (Offset > Buffer.Num()) {
        UE_LOG(LogTemp, Warning, TEXT("UByteBuffer::WriteByte - Buffer overflow. Cannot write byte value."));
        return this;
    }

    Buffer[Offset++] = Value;
    Length = FMath::Max(Length, Offset);
    return this;
}

UByteBuffer* UByteBuffer::WriteInt32(int32 Value)
{
    if (Offset + 4 > Buffer.Num()) {
        UE_LOG(LogTemp, Warning, TEXT("UByteBuffer::WriteInt32 - Buffer overflow. Cannot write int32 value."));
        return this;
    }
		
    Buffer[Offset++] = static_cast<uint8>(Value);
    Buffer[Offset++] = static_cast<uint8>(Value >> 8);
    Buffer[Offset++] = static_cast<uint8>(Value >> 16);
    Buffer[Offset++] = static_cast<uint8>(Value >> 24);

    Length = FMath::Max(Length, Offset);

    return this;
}

UByteBuffer* UByteBuffer::WriteInt64(int64 Value)
{
    if (Offset + 8 > Buffer.Num()) {
        UE_LOG(LogTemp, Warning, TEXT("UByteBuffer::WriteInt64 - Buffer overflow. Cannot write int64 value."));
        return this;
    }

    Buffer[Offset++] = static_cast<uint8>(Value);
    Buffer[Offset++] = static_cast<uint8>(Value >> 8);
    Buffer[Offset++] = static_cast<uint8>(Value >> 16);
    Buffer[Offset++] = static_cast<uint8>(Value >> 24);
    Buffer[Offset++] = static_cast<uint8>(Value >> 32);
    Buffer[Offset++] = static_cast<uint8>(Value >> 40);
    Buffer[Offset++] = static_cast<uint8>(Value >> 48);
    Buffer[Offset++] = static_cast<uint8>(Value >> 56);
    Length = FMath::Max(Length, Offset);
    return this;
}

uint8 UByteBuffer::ReadByte()
{
    if (Offset >= Length)    
        return 0;
    
    return Buffer[Offset++];
}

int32 UByteBuffer::ReadInt32()
{
    if (Buffer.Num() >= Offset + 4) {
        int32 result = 0;

        result |= static_cast<int32>(Buffer[Offset]) << 0;
        result |= static_cast<int32>(Buffer[Offset + 1]) << 8;
        result |= static_cast<int32>(Buffer[Offset + 2]) << 16;
        result |= static_cast<int32>(Buffer[Offset + 3]) << 24;

        Offset += 4;

        return result;
    }
    else {
        UE_LOG(LogTemp, Error, TEXT("Error packet %d"), Packet);
        return 0;
    }
}

int64 UByteBuffer::ReadInt64()
{
    if (Buffer.Num() >= Offset + 8) {
        int64 result = 0;
        result |= static_cast<int64>(Buffer[Offset]) << 0;
        result |= static_cast<int64>(Buffer[Offset + 1]) << 8;
        result |= static_cast<int64>(Buffer[Offset + 2]) << 16;
        result |= static_cast<int64>(Buffer[Offset + 3]) << 24;
        result |= static_cast<int64>(Buffer[Offset + 4]) << 32;
        result |= static_cast<int64>(Buffer[Offset + 5]) << 40;
        result |= static_cast<int64>(Buffer[Offset + 6]) << 48;
        result |= static_cast<int64>(Buffer[Offset + 7]) << 56;
        Offset += 8;
        return result;
    }
    else {
        UE_LOG(LogTemp, Error, TEXT("Error packet %d (int64)"), Packet);
        return 0;
    }
}

void UByteBuffer::Reset()
{
    Offset = 0;
    Length = 0;
}