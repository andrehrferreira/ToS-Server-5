#pragma once

#include "CoreMinimal.h"
#include "UByteBuffer.h"
#include <mutex>

class ByteBufferLinked
{
public:
    ByteBufferLinked();
    ~ByteBufferLinked();

    UByteBuffer* Head;
    UByteBuffer* Tail;

    void Add(UByteBuffer* Buffer);
    UByteBuffer* Clear();
    UByteBuffer* Take();
    int32 Length() const;
    void Merge(ByteBufferLinked& Other);
};
