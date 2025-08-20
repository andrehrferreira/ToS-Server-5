// This file was generated automatically, please do not change it.
#pragma once

#include "CoreMinimal.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "CookiePacket.generated.h"

USTRUCT(BlueprintType)
struct FCookiePacket
{
    GENERATED_USTRUCT_BODY();

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
    TArray<uint8> Cookie;

    int32 GetSize() const { return 49; }

    void Deserialize(UFlatBuffer* Buffer)
    {
        Cookie.SetNumUninitialized(48);
        Buffer->ReadBytes(Cookie.GetData(), 48);
    }
};
