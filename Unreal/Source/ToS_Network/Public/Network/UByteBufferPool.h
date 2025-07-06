#pragma once

#include "CoreMinimal.h"
#include "UByteBuffer.h"
#include "UByteBufferLinked.h"
#include <mutex>

class UByteBuffer;

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
