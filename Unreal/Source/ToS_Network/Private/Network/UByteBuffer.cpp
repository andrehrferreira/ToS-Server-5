#include "Network/UByteBuffer.h"
#include "Network/CRC32C.h"

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

void UByteBuffer::Assign(const TArray<uint8>& Source)
{
    int32 InLength = Source.Num();

    if (InLength <= 0)
    {
        UE_LOG(LogTemp, Error, TEXT("UByteBuffer::Assign - Invalid length."));
        return;
    }

    Buffer.SetNumUninitialized(InLength);
    FMemory::Memcpy(Buffer.GetData(), Source.GetData(), InLength);

    Offset = 0;
    Length = InLength;
}

//Write 

UByteBuffer* UByteBuffer::WriteByte(uint8 Value)
{
    if (Offset + 1 > Buffer.Num()) {
        UE_LOG(LogTemp, Warning, TEXT("UByteBuffer::WriteByte - Buffer overflow. Cannot write byte value."));
        return this;
    }

    Buffer[Offset++] = Value;
    Length = FMath::Max(Length, Offset);
    return this;
}

UByteBuffer* UByteBuffer::WriteUInt16(uint16 Value)
{
    if (Offset + 2 > Buffer.Num())
    {
        UE_LOG(LogTemp, Warning, TEXT("UByteBuffer::WriteUInt16 - Buffer overflow. Cannot write uint16 value."));
        return this;
    }

    Buffer[Offset++] = static_cast<uint8>(Value);
    Buffer[Offset++] = static_cast<uint8>(Value >> 8);

    Length = FMath::Max(Length, Offset);
    return this;
}

UByteBuffer* UByteBuffer::WriteInt32(int32 Value)
{
    if (Offset + 4 > Buffer.Num()) 
    {
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

UByteBuffer* UByteBuffer::WriteUInt32(uint32 Value)
{
    if (Offset + 4 > Buffer.Num())
    {
        UE_LOG(LogTemp, Warning, TEXT("UByteBuffer::WriteUInt32 - Buffer overflow. Cannot write uint32 value."));
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

UByteBuffer* UByteBuffer::WriteFloat(float Value)
{
    int32 IntValue = *(int32*)&Value;
    return WriteInt32(IntValue);
}

UByteBuffer* UByteBuffer::WriteBool(bool Value)
{
    return WriteByte(Value ? 1 : 0);
}

UByteBuffer* UByteBuffer::WriteString(const FString& Value)
{
    FTCHARToUTF8 Converter(*Value);
    int32 StringLength = Converter.Length();

    WriteInt32(StringLength);

    if (Offset + StringLength > Buffer.Num())
    {
        UE_LOG(LogTemp, Warning, TEXT("UByteBuffer::WriteString - Buffer overflow."));
        return this;
    }

    FMemory::Memcpy(Buffer.GetData() + Offset, (UTF8CHAR*)Converter.Get(), StringLength);
    Offset += StringLength;
    Length = FMath::Max(Length, Offset);

    return this;
}

UByteBuffer* UByteBuffer::WriteFVector(const FVector& Value)
{
    WriteInt32(static_cast<int32>(Value.X));
    WriteInt32(static_cast<int32>(Value.Y));
    WriteInt32(static_cast<int32>(Value.Z));
    return this;
}

UByteBuffer* UByteBuffer::WriteFRotator(const FRotator& Value)
{
    WriteInt32(static_cast<int32>(Value.Pitch));
    WriteInt32(static_cast<int32>(Value.Yaw));
    WriteInt32(static_cast<int32>(Value.Roll));
    return this;
}

void UByteBuffer::WriteSign()
{
    uint32 Crc = FCRC32C::Compute(Buffer.GetData(), Length);
    WriteUInt32(Crc);
}

// Read

uint8 UByteBuffer::ReadByte()
{
    if (Offset >= Length)    
        return 0;
    
    return Buffer[Offset++];
}

uint16 UByteBuffer::ReadUInt16()
{
    if (Buffer.Num() >= Offset + 2)
    {
        uint16 Result = 0;
        Result |= static_cast<uint16>(Buffer[Offset]);
        Result |= static_cast<uint16>(Buffer[Offset + 1]) << 8;

        Offset += 2;
        return Result;
    }
    else
    {
        UE_LOG(LogTemp, Error, TEXT("UByteBuffer::ReadUInt16 - Buffer underflow."));
        return 0;
    }
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

float UByteBuffer::ReadFloat()
{
    int32 IntValue = ReadInt32();
    return *(float*)&IntValue;
}

bool UByteBuffer::ReadBool()
{
    return ReadByte() != 0;
}

FString UByteBuffer::ReadString()
{
    int32 StringLength = ReadInt32();

    if (StringLength <= 0 || Offset + StringLength > Length)
    {
        UE_LOG(LogTemp, Warning, TEXT("UByteBuffer::ReadString - Invalid string length."));
        return FString();
    }

    FString Result = UTF8_TO_TCHAR(reinterpret_cast<const char*>(Buffer.GetData() + Offset));
    Offset += StringLength;
    return Result;
}

FVector UByteBuffer::ReadFVector()
{
    float X = static_cast<float>(ReadInt32());
    float Y = static_cast<float>(ReadInt32());
    float Z = static_cast<float>(ReadInt32());
    return FVector(X, Y, Z);
}

FRotator UByteBuffer::ReadFRotator()
{
    float Pitch = static_cast<float>(ReadInt32());
    float Yaw = static_cast<float>(ReadInt32());
    float Roll = static_cast<float>(ReadInt32());
    return FRotator(Pitch, Yaw, Roll);
}

// Utility

void UByteBuffer::Reset()
{
    Offset = 0;
    Length = 0;
}

void UByteBuffer::ResetOffset()
{
    Offset = 0;
}

FString UByteBuffer::ToHex() const
{
    uint32 Hash = 0;

    for (int32 i = 0; i < Offset; i++)    
        Hash = (Hash << 5) + Hash + Buffer[i];
    
    return FString::Printf(TEXT("%08X"), Hash);
}

int32 UByteBuffer::GetHashFast() const
{
    int32 Hash = 0;

    for (int32 i = 0; i < Offset; i++)    
        Hash = (Hash << 5) + Hash + Buffer[i];
    
    return Hash;
}

uint32 UByteBuffer::ReadUInt32()
{
    if (Buffer.Num() >= Offset + 4)
    {
        uint32 result = 0;
        result |= static_cast<uint32>(Buffer[Offset]);
        result |= static_cast<uint32>(Buffer[Offset + 1]) << 8;
        result |= static_cast<uint32>(Buffer[Offset + 2]) << 16;
        result |= static_cast<uint32>(Buffer[Offset + 3]) << 24;

        Offset += 4;
        return result;
    }
    else
    {
        UE_LOG(LogTemp, Error, TEXT("UByteBuffer::ReadUInt32 - Buffer underflow."));
        return 0;
    }
}

uint32 UByteBuffer::ReadSign()
{
    if (Length < 4)
    {
        UE_LOG(LogTemp, Error, TEXT("UByteBuffer::ReadSign - Buffer too small to contain CRC32C signature."));
        return 0;
    }

    int32 Index = Length - 4;
    uint32 value = static_cast<uint32>(Buffer[Index]) |
                   (static_cast<uint32>(Buffer[Index + 1]) << 8) |
                   (static_cast<uint32>(Buffer[Index + 2]) << 16) |
                   (static_cast<uint32>(Buffer[Index + 3]) << 24);

    return value;
}