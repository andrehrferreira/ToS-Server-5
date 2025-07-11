#include "Network/UFlatBuffer.h"
#include "Utils/CRC32C.h"

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

// Specialized Write methods for Blueprint compatibility
void UFlatBuffer::WriteByte(uint8 Value)
{
    Write<uint8>(Value);
}

void UFlatBuffer::WriteUInt16(uint16 Value)
{
    Write<uint16>(Value);
}

void UFlatBuffer::WriteInt32(int32 Value)
{
    Write<int32>(Value);
}

void UFlatBuffer::WriteUInt32(uint32 Value)
{
    Write<uint32>(Value);
}

void UFlatBuffer::WriteInt64(int64 Value)
{
    Write<int64>(Value);
}

void UFlatBuffer::WriteFloat(float Value)
{
    Write<float>(Value);
}

void UFlatBuffer::WriteBool(bool Value)
{
    Write<uint8>(Value ? 1 : 0);
}

void UFlatBuffer::WriteString(const FString& Value)
{
    FTCHARToUTF8 Converter(*Value);
    int32 StringLength = Converter.Length();

    // Write string length first
    Write<int32>(StringLength);

    // Write string data
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

// Specialized Read methods for Blueprint compatibility
uint8 UFlatBuffer::ReadByte()
{
    return Read<uint8>();
}

uint16 UFlatBuffer::ReadUInt16()
{
    return Read<uint16>();
}

int32 UFlatBuffer::ReadInt32()
{
    return Read<int32>();
}

uint32 UFlatBuffer::ReadUInt32()
{
    return Read<uint32>();
}

int64 UFlatBuffer::ReadInt64()
{
    return Read<int64>();
}

float UFlatBuffer::ReadFloat()
{
    return Read<float>();
}

bool UFlatBuffer::ReadBool()
{
    return Read<uint8>() != 0;
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

    // Create a temporary null-terminated string
    TArray<UTF8CHAR> TempBuffer;
    TempBuffer.SetNumUninitialized(StringLength + 1);
    FMemory::Memcpy(TempBuffer.GetData(), Data + Position, StringLength);
    TempBuffer[StringLength] = '\0';

    Position += StringLength;

    return FString(UTF8_TO_TCHAR(TempBuffer.GetData()));
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
