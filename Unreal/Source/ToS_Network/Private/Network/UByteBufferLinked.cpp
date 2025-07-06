#include "Network/UByteBufferLinked.h"
#include "Network/UByteBufferPool.h"
#include "Network/UByteBuffer.h"
#include <mutex>

ByteBufferLinked::ByteBufferLinked() { Head = nullptr; Tail = nullptr; }
ByteBufferLinked::~ByteBufferLinked() { Clear(); }

void ByteBufferLinked::Add(UByteBuffer* Buffer)
{
    if (!Buffer)
        return;
    Buffer->Next = Head;

    if (!Tail)
        Tail = Buffer;

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
    if (!Head)
        return nullptr;

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
