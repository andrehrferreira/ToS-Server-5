// Fill out your copyright notice in the Description page of Project Settings.

#include "Network/UByteBufferPool.h"
#include "Network/UByteBuffer.h"
#include <mutex>

ByteBufferLinked::ByteBufferLinked() { Head = nullptr; Tail = nullptr; }
ByteBufferLinked::~ByteBufferLinked() { Clear(); }

void ByteBufferLinked::Add(UByteBuffer* Buffer)
{
    if (!Buffer) return;
    Buffer->Next = Head;
    if (!Tail) Tail = Buffer;
    Head = Buffer;
}

UByteBuffer* ByteBufferLinked::Clear()
{
    UByteBuffer* Result = Head;
    Head = nullptr;
    Tail = nullptr;
    return Result;
}

UByteBuffer* ByteBufferLinked::Take()
{
    if (!Head) return nullptr;
    UByteBuffer* Result = Head;
    if (Head == Tail)
    {
        Head = nullptr;
        Tail = nullptr;
    }
    else
    {
        Head = Head->Next;
    }
    Result->Next = nullptr;
    return Result;
}

int32 ByteBufferLinked::Length() const
{
    int32 Val = 0;
    UByteBuffer* Current = Head;
    while (Current)
    {
        Current = Current->Next;
        ++Val;
    }
    return Val;
}

void ByteBufferLinked::Merge(ByteBufferLinked& Other)
{
    if (!Head)
    {
        Head = Other.Head;
        Tail = Other.Tail;
    }
    else if (Other.Head)
    {
        Tail->Next = Other.Head;
        Tail = Other.Tail;
    }
    Other.Head = nullptr;
    Other.Tail = nullptr;
}

// --- UByteBufferPool ---
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
