#pragma once
#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"

UENUM(BlueprintType)
enum class EConnectionStatus : uint8
{
    Disconnected     UMETA(DisplayName = "Disconnected"),
    Connecting       UMETA(DisplayName = "Connecting"),
    Connected        UMETA(DisplayName = "Connected"),
    ConnectionFailed UMETA(DisplayName = "Connection Failed")
};

UENUM(BlueprintType)
enum class EPacketType : uint8
{
    Connect             UMETA(DisplayName = "Connect"),
    Ping                UMETA(DisplayName = "Ping"),
    Pong                UMETA(DisplayName = "Pong"),
    Reliable            UMETA(DisplayName = "Reliable"),
    Unreliable          UMETA(DisplayName = "Unreliable"),
    Ack                 UMETA(DisplayName = "Ack"),
    Disconnect          UMETA(DisplayName = "Disconnect"),
    Error               UMETA(DisplayName = "Error"),
    ConnectionDenied    UMETA(DisplayName = "ConnectionDenied"),
    ConnectionAccepted  UMETA(DisplayName = "ConnectionAccepted")
};
