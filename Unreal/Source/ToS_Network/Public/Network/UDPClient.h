/*
 * UDPClient.h
 *
 * Author: Andre Ferreira
 *
 * Copyright (c) Uzmi Games. Licensed under the MIT License.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

#pragma once

#include "CoreMinimal.h"
#include "Sockets.h"
#include "SocketSubsystem.h"
#include "NetTypes.h"
#include "TimerManager.h"
#include <functional>
#include "HAL/Runnable.h"
#include "HAL/RunnableThread.h"
#include "HAL/ThreadSafeBool.h"

class UByteBuffer;
class UDPClient;

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
    ConnectionAccepted  UMETA(DisplayName = "ConnectionAccepted"),
    CheckIntegrity      UMETA(DisplayName = "CheckIntegrity")
};

class FPacketPollRunnable : public FRunnable
{
    friend class UDPClient;
public:
    FPacketPollRunnable(UDPClient* InClient) : Client(InClient), bStop(false) {}
    virtual uint32 Run() override;
    void Stop() override { bStop = true; }
    bool IsStopped() const { return bStop; }
private:
    UDPClient* Client;
    FThreadSafeBool bStop;
};

class UDPClient
{
public:
    UDPClient();
    ~UDPClient();

    std::function<void(UByteBuffer*)> OnDataReceive;
    std::function<void(int32)> OnConnect;
    std::function<void()> OnConnectDenied;
    std::function<void()> OnConnectionError;
    std::function<void()> OnDisconnect;

    bool Connect(const FString& Host, int32 Port);
    void Disconnect();
    void SendPong(int64 PingTime);
    void SendIntegrity(uint16 Code);
    void PollIncomingPackets();
    void SetConnectTimeout(float Seconds) { ConnectTimeout = Seconds; }
    void SetRetryInterval(float Seconds) { RetryInterval = Seconds; }
    void SetRetryEnabled(bool bEnabled) { bRetryEnabled = bEnabled; }
    float GetConnectTimeout() const { return ConnectTimeout; }
    float GetRetryInterval() const { return RetryInterval; }
    bool IsRetryEnabled() const { return bRetryEnabled; }

private:
    EConnectionStatus ConnectionStatus = EConnectionStatus::Disconnected;
    bool bIsConnected = false;
    bool bIsConnecting = false;
    float TimeSinceConnect = 0.0f;
    float TimeSinceLastRetry = 0.0f;
    float ConnectTimeout = 3.0f;
    float RetryInterval = 10.0f;
    bool bRetryEnabled = false;
    FString LastHost;
    int32 LastPort = 0;
    int32 RetryCount = 0;
    double LastPingTime = 0.0;

    // Socket
    FSocket* Socket = nullptr;
    TSharedPtr<FInternetAddr> RemoteEndpoint;

    // Timer
    FTimerHandle RetryTimerHandle;
    void StartRetryTimer();
    void StopRetryTimer();
    void OnRetryTimerTick();

    // Thread de polling
    FPacketPollRunnable* PacketPollRunnable = nullptr;
    FRunnableThread* PacketPollThread = nullptr;
    void StartPacketPollThread();
    void StopPacketPollThread();
};
