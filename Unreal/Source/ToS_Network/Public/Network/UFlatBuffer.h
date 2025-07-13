/*
 * FlatBuffer
 *
 * Author: Andre Ferreira
 * Modified from original UByteBuffer implementation
 *
 * Copyright (c) Uzmi Games. Licensed under the MIT License.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "UFlatBuffer.generated.h"

UCLASS()
class TOS_NETWORK_API UFlatBuffer : public UObject
{
	GENERATED_BODY()

public:
	UFUNCTION(BlueprintPure, Category = "FlatBuffer")
        static UFlatBuffer* CreateFlatBuffer(int32 Capacity = 1500);

	UFUNCTION(BlueprintPure, Category = "FlatBuffer")
	static UFlatBuffer* CreateFromData(const TArray<uint8>& Data);

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	void Initialize(int32 Capacity);

	// Template-based Write operations
	template<typename T>
	void Write(const T& Value)
	{
		static_assert(std::is_trivial_v<T>, "Type must be trivial for unsafe operations");

		int32 Size = sizeof(T);
		if (Position + Size > Capacity)
		{
			UE_LOG(LogTemp, Warning, TEXT("UFlatBuffer::Write - Buffer overflow. Cannot write value of size %d"), Size);
			return;
		}

		FMemory::Memcpy(Data + Position, &Value, Size);
		Position += Size;
	}

	// Template-based Read operations
	template<typename T>
	T Read()
	{
		static_assert(std::is_trivial_v<T>, "Type must be trivial for unsafe operations");

		int32 Size = sizeof(T);
		if (Position + Size > Capacity)
		{
			UE_LOG(LogTemp, Error, TEXT("UFlatBuffer::Read - Buffer underflow. Cannot read value of size %d"), Size);
			return T{};
		}

		T Value;
		FMemory::Memcpy(&Value, Data + Position, Size);
		Position += Size;
		return Value;
	}

	// Specialized Write methods for Blueprint compatibility
	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	void WriteByte(uint8 Value);

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	void WriteUInt16(uint16 Value);

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	void WriteInt32(int32 Value);

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	void WriteUInt32(uint32 Value);

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        void WriteInt64(int64 Value);

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        void WriteVarInt(int32 Value);

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        void WriteVarLong(int64 Value);

        void WriteVarUInt(uint32 Value);

        void WriteVarULong(uint64 Value);

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	void WriteFloat(float Value);

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        void WriteBool(bool Value);

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        void WriteBit(bool Value);

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        void WriteAsciiString(const FString& Value);

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        void WriteUtf8String(const FString& Value);

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	void WriteString(const FString& Value);

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	void WriteFVector(const FVector& Value);

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	void WriteFRotator(const FRotator& Value);

	// Specialized Read methods for Blueprint compatibility
	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	uint8 ReadByte();

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	uint16 ReadUInt16();

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	int32 ReadInt32();

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	uint32 ReadUInt32();

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        int64 ReadInt64();

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        int32 ReadVarInt();

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        int64 ReadVarLong();

        uint32 ReadVarUInt();

        uint64 ReadVarULong();

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	float ReadFloat();

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        bool ReadBool();

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        bool ReadBit();

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        FString ReadAsciiString();

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        FString ReadUtf8String();

        UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
        void AlignBits();

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	FString ReadString();

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	FVector ReadFVector();

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	FRotator ReadFRotator();

	// Utility methods
	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	void Reset();

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	void Free();

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	FString ToHex() const;

	UFUNCTION(BlueprintCallable, Category = "FlatBuffer")
	uint32 GetHashFast() const;

	// Direct memory access methods
	void CopyFromMemory(const uint8* SourceData, int32 Length);
	void CopyToMemory(uint8* DestData, int32 Length) const;

	// Accessors
	FORCEINLINE uint8* GetData() { return Data; }
	FORCEINLINE const uint8* GetData() const { return Data; }
	FORCEINLINE int32 GetPosition() const { return Position; }
	FORCEINLINE int32 GetCapacity() const { return Capacity; }
	FORCEINLINE bool IsDisposed() const { return bDisposed; }

	FORCEINLINE void SetPosition(int32 NewPosition)
	{
		Position = FMath::Clamp(NewPosition, 0, Capacity);
	}

	// Raw buffer access for compatibility
	uint8* GetRawBuffer() { return Data; }
	int32 GetLength() const { return Position; }
	int32 GetOffset() const { return Position; }
        void SetOffset(int32 NewOffset) { SetPosition(NewOffset); }

        template<typename T>
        FORCEINLINE T Peek() const
        {
                static_assert(std::is_trivial_v<T>, "Type must be trivial");
                int32 Size = sizeof(T);
                if (Position + Size > Capacity)
                {
                        UE_LOG(LogTemp, Error, TEXT("UFlatBuffer::Peek - Buffer underflow."));
                        return T{};
                }
                T Value;
                FMemory::Memcpy(&Value, Data + Position, Size);
                return Value;
        }

protected:
	virtual void BeginDestroy() override;

private:
        uint8* Data;
        int32 Capacity;
        int32 Position;
        bool bDisposed;
        uint8 WriteBits = 0;
        uint8 WriteBitIndex = 0;
        uint8 ReadBits = 0;
        uint8 ReadBitIndex = 0;

        void EnsureCapacity(int32 RequiredSize);
};

// Template specializations for common Unreal types
template<>
inline void UFlatBuffer::Write<FVector>(const FVector& Value)
{
	Write<int32>(static_cast<int32>(Value.X));
	Write<int32>(static_cast<int32>(Value.Y));
	Write<int32>(static_cast<int32>(Value.Z));
}

template<>
inline FVector UFlatBuffer::Read<FVector>()
{
	int32 X = Read<int32>();
	int32 Y = Read<int32>();
	int32 Z = Read<int32>();
	return FVector(static_cast<float>(X), static_cast<float>(Y), static_cast<float>(Z));
}

template<>
inline void UFlatBuffer::Write<FRotator>(const FRotator& Value)
{
        Write<int32>(static_cast<int32>(Value.Pitch));
        Write<int32>(static_cast<int32>(Value.Yaw));
        Write<int32>(static_cast<int32>(Value.Roll));
}

template<>
inline FRotator UFlatBuffer::Read<FRotator>()
{
        int32 Pitch = Read<int32>();
        int32 Yaw = Read<int32>();
        int32 Roll = Read<int32>();
        return FRotator(static_cast<float>(Pitch), static_cast<float>(Yaw), static_cast<float>(Roll));
}

template<>
inline void UFlatBuffer::Write<int32>(const int32& Value)
{
        WriteVarInt(Value);
}

template<>
inline int32 UFlatBuffer::Read<int32>()
{
        return ReadVarInt();
}

template<>
inline void UFlatBuffer::Write<uint32>(const uint32& Value)
{
        WriteVarUInt(Value);
}

template<>
inline uint32 UFlatBuffer::Read<uint32>()
{
        return ReadVarUInt();
}

template<>
inline void UFlatBuffer::Write<int64>(const int64& Value)
{
        WriteVarLong(Value);
}

template<>
inline int64 UFlatBuffer::Read<int64>()
{
        return ReadVarLong();
}

template<>
inline void UFlatBuffer::Write<uint64>(const uint64& Value)
{
        WriteVarULong(Value);
}

template<>
inline uint64 UFlatBuffer::Read<uint64>()
{
        return ReadVarULong();
}
