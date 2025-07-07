#pragma once

#include "CoreMinimal.h"

class FCRC32C
{
public:
    static const uint32 ChecksumSize = 4;

    static uint32 Compute(const uint8* Data, int32 Length);
    static uint32 Compute(const TArray<uint8>& Data)
    {
        return Compute(Data.GetData(), Data.Num());
    }
};
