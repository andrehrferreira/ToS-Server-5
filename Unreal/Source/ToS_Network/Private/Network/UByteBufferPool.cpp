#include "Network/UByteBufferPool.h"
#include "Network/UByteBuffer.h"
#include "Network/UByteBufferLinked.h"
#include <mutex>

ByteBufferLinked UByteBufferPool::Global;
static thread_local ByteBufferLinked* Local = nullptr;
std::mutex UByteBufferPool::GlobalMutex;

UByteBufferPool::UByteBufferPool() {}
UByteBufferPool::~UByteBufferPool() {}

UByteBuffer* UByteBufferPool::Acquire()
{
    UByteBuffer* Buffer = nullptr;

    if (!Local)
    {
        std::lock_guard<std::mutex> lock(GlobalMutex);
        Buffer = Global.Take();
    }
    else
    {
        Buffer = Local->Take();

        if (!Buffer)
        {
            std::lock_guard<std::mutex> lock(GlobalMutex);
            Buffer = Global.Take();
        }
    }

    if (!Buffer)
        Buffer = UByteBuffer::CreateEmptyByteBuffer();

    return Buffer;
}

void UByteBufferPool::Release(UByteBuffer* Buffer)
{
    if (!Local)
        Local = new ByteBufferLinked();

    Buffer->Reset();
    Local->Add(Buffer);
}

void UByteBufferPool::Merge()
{
    if (Local && Local->Head)
    {
        std::lock_guard<std::mutex> lock(GlobalMutex);
        Global.Merge(*Local);
    }
}

UByteBuffer* UByteBufferPool::Clear()
{
    std::lock_guard<std::mutex> lock(GlobalMutex);
    return Global.Clear();
}
