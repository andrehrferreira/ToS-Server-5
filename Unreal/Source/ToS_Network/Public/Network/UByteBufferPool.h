#pragma once

#include "CoreMinimal.h"
#include "UByteBuffer.h"
#include <mutex>

class UByteBuffer;

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

class TOS_NETWORK_API UByteBufferPool
{
public:
    UByteBufferPool();
    virtual ~UByteBufferPool();

    static UByteBuffer* Acquire();
    static void Release(UByteBuffer* Buffer);
    static void Merge();
    static UByteBuffer* Clear();

private:
    static ByteBufferLinked Global;
    static std::mutex GlobalMutex;
};
