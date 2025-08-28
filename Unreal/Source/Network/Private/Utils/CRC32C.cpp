#include "Utils/CRC32C.h"

#if PLATFORM_WINDOWS || PLATFORM_LINUX
#include <immintrin.h> // SSE4.2
#endif

#if PLATFORM_MAC || PLATFORM_IOS
#include <arm_acle.h>
#endif

uint32 FCRC32C::Table[16 * 256] = { 0 };
bool FCRC32C::bTableInitialized = false;

void FCRC32C::InitializeTable()
{
    if (bTableInitialized)
        return;

    for (uint32 i = 0; i < 256; i++)
    {
        uint32 res = i;
        for (int t = 0; t < 16; t++)
        {
            for (int k = 0; k < 8; k++)
                res = (res & 1) ? (Polynomial ^ (res >> 1)) : (res >> 1);
            Table[t * 256 + i] = res;
        }
    }

    bTableInitialized = true;
}

bool FCRC32C::HasSSE42()
{
#if PLATFORM_WINDOWS
    int CPUInfo[4] = { -1 };
    __cpuid(CPUInfo, 1);
    return (CPUInfo[2] & (1 << 20)) != 0;
#elif PLATFORM_LINUX || PLATFORM_MAC
    return __builtin_cpu_supports("sse4.2");
#else
    return false;
#endif
}

bool FCRC32C::HasARMCRC()
{
#if PLATFORM_MAC || PLATFORM_IOS
    return true;
#else
    return false;
#endif
}

uint32 FCRC32C::Compute(const uint8* Data, int32 Length)
{
    uint32 crcLocal = 0xFFFFFFFFu;

    if (HasSSE42())
    {
        return ComputeSSE42(Data, Length, crcLocal) ^ 0xFFFFFFFFu;
    }
    else if (HasARMCRC())
    {
        return ComputeARMCRC(Data, Length, crcLocal) ^ 0xFFFFFFFFu;
    }
    else
    {
        InitializeTable();
        return ComputeFallback(Data, Length, crcLocal) ^ 0xFFFFFFFFu;
    }
}

uint32 FCRC32C::ComputeSSE42(const uint8* Data, int32 Length, uint32 crcLocal)
{
#if PLATFORM_WINDOWS || PLATFORM_LINUX
    const uint8* p = Data;
    int remaining = Length;

    uint64 crcLocal64 = crcLocal; 

    while (remaining >= 8)
    {
        crcLocal64 = _mm_crc32_u64(crcLocal64, *(const uint64*)p);
        p += 8;
        remaining -= 8;
    }

    crcLocal = (uint32)crcLocal64;

    while (remaining--)
    {
        crcLocal = _mm_crc32_u8(crcLocal, *p++);
    }

    return crcLocal;
#else
    return ComputeFallback(Data, Length, crcLocal);
#endif
}

uint32 FCRC32C::ComputeARMCRC(const uint8* Data, int32 Length, uint32 crcLocal)
{
#if PLATFORM_MAC || PLATFORM_IOS
    const uint8* p = Data;
    int remaining = Length;

    uint64 crcLocal64 = crcLocal; 

    while (remaining >= 8)
    {
        crcLocal64 = __crc32cd(crcLocal64, *(const uint64*)p);
        p += 8;
        remaining -= 8;
    }

    crcLocal = (uint32)crcLocal64;

    while (remaining >= 4)
    {
        crcLocal = __crc32cw(crcLocal, *(const uint32*)p);
        p += 4;
        remaining -= 4;
    }

    while (remaining--)
    {
        crcLocal = __crc32cb(crcLocal, *p++);
    }

    return crcLocal;
#else
    return ComputeFallback(Data, Length, crcLocal);
#endif
}

uint32 FCRC32C::ComputeFallback(const uint8* Data, int32 Length, uint32 crcLocal)
{
    int offset = 0;
    int remaining = Length;

    while (remaining >= 16)
    {
        const uint8* current = Data + offset;

        uint32 a = Table[(3 * 256) + current[12]]
            ^ Table[(2 * 256) + current[13]]
            ^ Table[(1 * 256) + current[14]]
            ^ Table[(0 * 256) + current[15]];

        uint32 b = Table[(7 * 256) + current[8]]
            ^ Table[(6 * 256) + current[9]]
            ^ Table[(5 * 256) + current[10]]
            ^ Table[(4 * 256) + current[11]];

        uint32 c = Table[(11 * 256) + current[4]]
            ^ Table[(10 * 256) + current[5]]
            ^ Table[(9 * 256) + current[6]]
            ^ Table[(8 * 256) + current[7]];

        uint32 d = Table[(15 * 256) + ((uint8)crcLocal ^ current[0])]
            ^ Table[(14 * 256) + ((uint8)(crcLocal >> 8) ^ current[1])]
            ^ Table[(13 * 256) + ((uint8)(crcLocal >> 16) ^ current[2])]
            ^ Table[(12 * 256) + ((crcLocal >> 24) ^ current[3])];

        crcLocal = d ^ c ^ b ^ a;

        offset += 16;
        remaining -= 16;
    }

    while (remaining--)
    {
        crcLocal = Table[(crcLocal ^ Data[offset++]) & 0xFF] ^ (crcLocal >> 8);
    }

    return crcLocal;
}
