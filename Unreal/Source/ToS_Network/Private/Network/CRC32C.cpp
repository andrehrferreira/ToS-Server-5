#include "Network/CRC32C.h"

static const uint32 Polynomial = 0x82F63B78u;

uint32 FCRC32C::Compute(const uint8* Data, int32 Length)
{
    uint32 Crc = 0xFFFFFFFFu;
    for (int32 i = 0; i < Length; ++i)
    {
        Crc ^= Data[i];
        for (int j = 0; j < 8; ++j)
        {
            uint32 Mask = -(int32)(Crc & 1);
            Crc = (Crc >> 1) ^ (Polynomial & Mask);
        }
    }
    return ~Crc;
}
