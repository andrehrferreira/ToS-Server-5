#pragma once

#include "CoreMinimal.h"

class FCRC32C
{
public:
    static uint32 Compute(const uint8* Data, int32 Length);

private:
    static const uint32 Polynomial = 0x82F63B78u;
    static uint32 Table[16 * 256];
    static bool bTableInitialized;
    static void InitializeTable();

    static bool HasSSE42();
    static bool HasARMCRC();

    static uint32 ComputeSSE42(const uint8* Data, int32 Length, uint32 crcLocal);
    static uint32 ComputeARMCRC(const uint8* Data, int32 Length, uint32 crcLocal);
    static uint32 ComputeFallback(const uint8* Data, int32 Length, uint32 crcLocal);
};
