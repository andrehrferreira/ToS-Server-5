#include "Network/UFlatBuffer.h"
#include "Utils/CRC32C.h"
#include "Containers/StringConv.h"

static uint32 EncodeZigZag32(int32 Value)
{
    return static_cast<uint32>((Value << 1) ^ (Value >> 31));
}

static int32 DecodeZigZag32(uint32 Value)
{
    return static_cast<int32>((Value >> 1) ^ -static_cast<int32>(Value & 1));
}

static uint64 EncodeZigZag64(int64 Value)
{
    return (static_cast<uint64>(Value) << 1) ^ static_cast<uint64>(Value >> 63);
}

static int64 DecodeZigZag64(uint64 Value)
{
    return static_cast<int64>((Value >> 1) ^ -static_cast<int64>(Value & 1));
}

UFlatBuffer* UFlatBuffer::CreateFlatBuffer(int32 Capacity)
{
    UFlatBuffer* FlatBuffer = NewObject<UFlatBuffer>();
    FlatBuffer->Initialize(Capacity);
    return FlatBuffer;
}

UFlatBuffer* UFlatBuffer::CreateFromData(const TArray<uint8>& InData)
{
    UFlatBuffer* FlatBuffer = NewObject<UFlatBuffer>();
    int32 DataSize = InData.Num();
    FlatBuffer->Initialize(DataSize);

    if (DataSize > 0)
    {
        FlatBuffer->CopyFromMemory(InData.GetData(), DataSize);
        FlatBuffer->Position = DataSize;
    }

    return FlatBuffer;
}

void UFlatBuffer::Initialize(int32 InCapacity)
{
    if (InCapacity <= 0)
    {
        UE_LOG(LogTemp, Error, TEXT("UFlatBuffer::Initialize - Invalid capacity: %d"), InCapacity);
        return;
    }

    if (Data != nullptr)
    {
        FMemory::Free(Data);
    }

    Capacity = InCapacity;
    Position = 0;
    bDisposed = false;
    Data = static_cast<uint8*>(FMemory::Malloc(Capacity));

    if (Data == nullptr)
    {
        UE_LOG(LogTemp, Error, TEXT("UFlatBuffer::Initialize - Failed to allocate memory for capacity: %d"), Capacity);
        bDisposed = true;
    }
}

void UFlatBuffer::BeginDestroy()
{
    Free();
    Super::BeginDestroy();
}

void UFlatBuffer::Free()
{
    if (Data != nullptr && !bDisposed)
    {
        FMemory::Free(Data);
        Data = nullptr;
        bDisposed = true;
    }
}

void UFlatBuffer::Reset()
{
    Position = 0;
}

void UFlatBuffer::EnsureCapacity(int32 RequiredSize)
{
    if (RequiredSize > Capacity)
    {
        UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::EnsureCapacity - Required size %d exceeds capacity %d"), RequiredSize, Capacity);
    }
}

void UFlatBuffer::CopyFromMemory(const uint8* SourceData, int32 Length)
{
    if (SourceData == nullptr || Length <= 0)
    {
        UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::CopyFromMemory - Invalid source data or length"));
        return;
    }

    if (Length > Capacity)
    {
        UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::CopyFromMemory - Source length %d exceeds buffer capacity %d"), Length, Capacity);
        Length = Capacity;
    }

    FMemory::Memcpy(Data, SourceData, Length);
    Position = 0;
}

void UFlatBuffer::CopyToMemory(uint8* DestData, int32 Length) const
{
    if (DestData == nullptr || Length <= 0)
    {
        UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::CopyToMemory - Invalid destination data or length"));
        return;
    }

    int32 CopyLength = FMath::Min(Length, Position);
    FMemory::Memcpy(DestData, Data, CopyLength);
}

void UFlatBuffer::WriteByte(uint8 Value)
{
    Write<uint8>(Value);
}

void UFlatBuffer::WriteUInt16(uint16 Value)
{
    Write<uint16>(Value);
}

void UFlatBuffer::WriteInt16(int16 Value)
{
    Write<int16>(Value);
}

void UFlatBuffer::WriteInt32(int32 Value)
{
    WriteVarInt(Value);
}

void UFlatBuffer::WriteUInt32(uint32 Value)
{
    WriteVarUInt(Value);
}

void UFlatBuffer::WriteInt64(int64 Value)
{
    WriteVarLong(Value);
}

void UFlatBuffer::WriteVarInt(int32 Value)
{
    uint32 V = EncodeZigZag32(Value);

    while (V >= 0x80)
    {
        Write<uint8>(static_cast<uint8>(V | 0x80));
        V >>= 7;
    }

    Write<uint8>(static_cast<uint8>(V));
}

void UFlatBuffer::WriteVarLong(int64 Value)
{
    uint64 V = EncodeZigZag64(Value);

    while (V >= 0x80)
    {
        Write<uint8>(static_cast<uint8>(V | 0x80));
        V >>= 7;
    }

    Write<uint8>(static_cast<uint8>(V));
}

void UFlatBuffer::WriteVarUInt(uint32 Value)
{
    uint32 V = Value;

    while (V >= 0x80)
    {
        Write<uint8>(static_cast<uint8>(V | 0x80));
        V >>= 7;
    }

    Write<uint8>(static_cast<uint8>(V));
}

void UFlatBuffer::WriteVarULong(uint64 Value)
{
    uint64 V = Value;

    while (V >= 0x80)
    {
        Write<uint8>(static_cast<uint8>(V | 0x80));
        V >>= 7;
    }

    Write<uint8>(static_cast<uint8>(V));
}

void UFlatBuffer::WriteFloat(float Value)
{
    Write<float>(Value);
}

void UFlatBuffer::WriteBool(bool Value)
{
    Write<uint8>(Value ? 1 : 0);
}

void UFlatBuffer::WriteBit(bool Value)
{
    if (WriteBitIndex == 0)
    {
        if (Position >= Capacity)
            return;
        Data[Position] = 0;
    }

    if (Value)
        Data[Position] |= 1 << WriteBitIndex;

    WriteBitIndex++;
    if (WriteBitIndex == 8)
    {
        WriteBitIndex = 0;
        Position++;
    }
}

void UFlatBuffer::WriteAsciiString(const FString& Value)
{
    int32 StringLength = Value.Len();
    Write<int32>(StringLength);

    if (StringLength > 0)
    {
        if (Position + StringLength > Capacity)
        {
            UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::WriteAsciiString - Buffer overflow. String length: %d"), StringLength);
            return;
        }

        for (int32 i = 0; i < StringLength; ++i)
        {
            TCHAR Char = Value[i];
            Data[Position + i] = (Char >= 0 && Char <= 127) ? static_cast<uint8>(Char) : '?';
        }

        Position += StringLength;
    }
}

void UFlatBuffer::WriteBytes(const uint8* Source, int32 Length)
{
    if (!Source || Length <= 0)
        return;

    if (Position + Length > Capacity)
        return;

    FMemory::Memcpy(Data + Position, Source, Length);
    Position += Length;
}

void UFlatBuffer::WriteUtf8String(const FString& Value)
{
    WriteString(Value);
}

void UFlatBuffer::WriteString(const FString& Value)
{
    FTCHARToUTF8 Converter(*Value);
    int32 StringLength = Converter.Length();
    Write<int32>(StringLength);

    if (StringLength > 0)
    {
        if (Position + StringLength > Capacity)
        {
            UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::WriteString - Buffer overflow. String length: %d"), StringLength);
            return;
        }

        FMemory::Memcpy(Data + Position, (UTF8CHAR*)Converter.Get(), StringLength);
        Position += StringLength;
    }
}

void UFlatBuffer::WriteFVector(const FVector& Value)
{
    Write<FVector>(Value);
}

void UFlatBuffer::WriteFRotator(const FRotator& Value)
{
    Write<FRotator>(Value);
}

uint8 UFlatBuffer::ReadByte()
{
    return Read<uint8>();
}

uint16 UFlatBuffer::ReadUInt16()
{
    return Read<uint16>();
}

int16 UFlatBuffer::ReadInt16()
{
    return Read<int16>();
}

int32 UFlatBuffer::ReadInt32()
{
    return ReadVarInt();
}

uint32 UFlatBuffer::ReadUInt32()
{
    return ReadVarUInt();
}

int64 UFlatBuffer::ReadInt64()
{
    return ReadVarLong();
}

int32 UFlatBuffer::ReadVarInt()
{
    int32 Shift = 0;
    uint32 Result = 0;

    while (true)
    {
        uint8 B = Read<uint8>();
        Result |= static_cast<uint32>(B & 0x7F) << Shift;

        if ((B & 0x80) == 0)
            break;

        Shift += 7;

        if (Shift > 35)
        {
            UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::ReadVarInt - VarInt too long"));
            break;
        }
    }

    return DecodeZigZag32(Result);
}

int64 UFlatBuffer::ReadVarLong()
{
    int32 Shift = 0;
    uint64 Result = 0;

    while (true)
    {
        uint8 B = Read<uint8>();
        Result |= static_cast<uint64>(B & 0x7F) << Shift;

        if ((B & 0x80) == 0)
            break;

        Shift += 7;

        if (Shift > 70)
        {
            UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::ReadVarLong - VarLong too long"));
            break;
        }
    }

    return DecodeZigZag64(Result);
}

uint32 UFlatBuffer::ReadVarUInt()
{
    int32 Shift = 0;
    uint32 Result = 0;

    while (true)
    {
        uint8 B = Read<uint8>();
        Result |= static_cast<uint32>(B & 0x7F) << Shift;

        if ((B & 0x80) == 0)
            break;

        Shift += 7;

        if (Shift > 35)
        {
            UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::ReadVarUInt - VarUInt too long"));
            break;
        }
    }

    return Result;
}

uint64 UFlatBuffer::ReadVarULong()
{
    int32 Shift = 0;
    uint64 Result = 0;

    while (true)
    {
        uint8 B = Read<uint8>();
        Result |= static_cast<uint64>(B & 0x7F) << Shift;

        if ((B & 0x80) == 0)
            break;

        Shift += 7;

        if (Shift > 70)
        {
            UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::ReadVarULong - VarULong too long"));
            break;
        }
    }

    return Result;
}

float UFlatBuffer::ReadFloat()
{
    return Read<float>();
}

bool UFlatBuffer::ReadBool()
{
    return Read<uint8>() != 0;
}

bool UFlatBuffer::ReadBit()
{
    if (ReadBitIndex == 0)
        ReadBits = Read<uint8>();

    bool b = (ReadBits & (1 << ReadBitIndex)) != 0;
    ReadBitIndex++;

    if (ReadBitIndex == 8)
        ReadBitIndex = 0;

    return b;
}

FString UFlatBuffer::ReadString()
{
    int32 StringLength = Read<int32>();

    if (StringLength <= 0)
    {
        return FString();
    }

    if (Position + StringLength > Capacity)
    {
        UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::ReadString - Buffer underflow. String length: %d"), StringLength);
        return FString();
    }

    TArray<UTF8CHAR> TempBuffer;
    TempBuffer.SetNumUninitialized(StringLength + 1);
    FMemory::Memcpy(TempBuffer.GetData(), Data + Position, StringLength);
    TempBuffer[StringLength] = static_cast<UTF8CHAR>(0);
    Position += StringLength;

    return FString(UTF8_TO_TCHAR(TempBuffer.GetData()));
}

FString UFlatBuffer::ReadAsciiString()
{
    int32 StringLength = Read<int32>();

    if (StringLength <= 0)
    {
        return FString();
    }

    if (Position + StringLength > Capacity)
    {
        UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::ReadAsciiString - Buffer underflow. String length: %d"), StringLength);
        return FString();
    }

    TArray<ANSICHAR> Temp;
    Temp.SetNumUninitialized(StringLength + 1);
    FMemory::Memcpy(Temp.GetData(), Data + Position, StringLength);
    Temp[StringLength] = 0;
    Position += StringLength;

    return FString(ANSI_TO_TCHAR(Temp.GetData()));
}

FString UFlatBuffer::ReadUtf8String()
{
    return ReadString();
}

uint32 UFlatBuffer::ReadSign()
{
    if (Capacity < 4)
    {
        UE_LOG(LogTemp, Error, TEXT("UFlatBuffer::ReadSign - Buffer too small (%d bytes)"), Capacity);
        return 0;
    }

    int32 SignOffset = Capacity - 4;
    uint32 Signature = 0;
    FMemory::Memcpy(&Signature, Data + SignOffset, 4);

    Capacity -= 4;

    return Signature;
}

void UFlatBuffer::AlignBits()
{
    if (WriteBitIndex > 0)
    {
        WriteBitIndex = 0;
        Position++;
    }

    if (ReadBitIndex > 0)
        ReadBitIndex = 0;
}

FVector UFlatBuffer::ReadFVector()
{
    return Read<FVector>();
}

FRotator UFlatBuffer::ReadFRotator()
{
    return Read<FRotator>();
}

FString UFlatBuffer::ToHex() const
{
    uint32 Hash = 0;

    for (int32 i = 0; i < Position; i++)
        Hash = (Hash << 5) + Hash + Data[i];

    return FString::Printf(TEXT("%08X"), Hash);
}

uint32 UFlatBuffer::GetHashFast() const
{
    uint32 Hash = 0;

    for (int32 i = 0; i < Position; i++)
        Hash = (Hash << 5) + Hash + Data[i];

    return Hash;
}


void UFlatBuffer::PrintBuffer(const uint8* buffer, int len)
{
    FString HexString;

    for (int i = 0; i < len; ++i)
    {
        HexString += FString::Printf(TEXT("%02X "), buffer[i]);
    }

    UE_LOG(LogTemp, Log, TEXT("PrintBuffer: %s"), *HexString);
}