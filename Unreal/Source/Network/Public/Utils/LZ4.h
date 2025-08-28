#pragma once

#include "CoreMinimal.h"

class FLZ4
{
public:
    static int32 Compress(const uint8* Src, int32 SrcSize, uint8* Dst, int32 DstCapacity);
    static int32 Decompress(const uint8* Src, int32 SrcSize, uint8* Dst, int32 DstCapacity);

private:
    static constexpr int MINMATCH = 4;
    static constexpr int HASH_LOG = 16;
    static constexpr int HASH_SIZE = 1 << HASH_LOG;
};
