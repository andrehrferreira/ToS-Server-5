/*
 * ByteBuffer
 *
 * Author: Andre Ferreira
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
#include "UByteBuffer.generated.h"

UCLASS()
class TOS_NETWORK_API UByteBuffer : public UObject
{
	GENERATED_BODY()
	
public:
	UFUNCTION(BlueprintPure, Category = "ByteBuffer")
	static UByteBuffer* CreateEmptyByteBuffer(int32 Capacity = 3600);

	UFUNCTION(BlueprintPure, Category = "ByteBuffer")
	static UByteBuffer* CreateByteBuffer(const TArray<uint8>& Data);

	UFUNCTION(BlueprintPure, Category = "ByteBuffer")
	void Assign(const TArray<uint8>& Source);

	// Write

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	UByteBuffer* WriteByte(uint8 Value);

	UByteBuffer* WriteUInt16(uint16 Value);

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	UByteBuffer* WriteInt32(int32 Value);

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	UByteBuffer* WriteInt64(int64 Value);

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	UByteBuffer* WriteFloat(float Value);

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	UByteBuffer* WriteBool(bool Value);

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	UByteBuffer* WriteString(const FString& Value);

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	UByteBuffer* WriteFVector(const FVector& Value);

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	UByteBuffer* WriteFRotator(const FRotator& Value);

	// Read

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	uint8 ReadByte();	

	uint16 ReadUInt16();

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	int32 ReadInt32();

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	int64 ReadInt64();

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	float ReadFloat();

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	bool ReadBool();

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	FString ReadString();

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	FVector ReadFVector();

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	FRotator ReadFRotator();

	// Utility

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	void Reset();

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	FString ToHex() const;

	UFUNCTION(BlueprintCallable, Category = "ByteBuffer")
	int32 GetHashFast() const;

	FORCEINLINE uint8* GetRawBuffer() { return Buffer.GetData(); }
	FORCEINLINE int32 GetLength() const { return Length; }
	FORCEINLINE int32 GetOffset() const { return Offset; }
	FORCEINLINE void SetOffset(int32 NewOffset) { Offset = NewOffset; }
	FORCEINLINE void SetLength(int32 NewLength) { Length = NewLength; }

	void SetDataFromRaw(const uint8* Data, int32 DataSize)
	{
		Buffer.SetNumUninitialized(DataSize);

		if (DataSize > 0 && Data)
			FMemory::Memcpy(Buffer.GetData(), Data, DataSize);

		Length = DataSize;
		Offset = 0;
	}

	UByteBuffer* Next = nullptr; 

private:
	TArray<uint8> Buffer;
	int32 Offset = 0;
	int32 Length = 0;
	uint8 Packet = 0;
};
