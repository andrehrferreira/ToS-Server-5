#include "Network/ENetSubsystem.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "Misc/ScopeLock.h"
#include "Sockets.h"
#include "SocketSubsystem.h"
#include "IPAddress.h"
#include "Common/UdpSocketBuilder.h"
//%INCLUDES%

void UENetSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
    UdpClient = MakeUnique<UDPClient>();

    UdpClient->OnDataReceive = [this](UFlatBuffer* Buffer)
    {
        if (Buffer)
        {
            EServerPackets PacketType = static_cast<EServerPackets>(Buffer->ReadInt16());

            switch (PacketType) {
//%DATASWITCH%
            }
        }
    };

    UdpClient->OnConnect = [this](int32 clientId)
    {
        OnConnect.Broadcast(clientId);

        if (!TickHandle.IsValid())
        {
            TickHandle = FTSTicker::GetCoreTicker().AddTicker(
                FTickerDelegate::CreateUObject(this, &UENetSubsystem::Tick),
                0.0f
            );
        }
    };

    UdpClient->OnConnectDenied = [this]()
    {
        OnConnectDenied.Broadcast();

        if (TickHandle.IsValid())
        {
            FTSTicker::GetCoreTicker().RemoveTicker(TickHandle);
            TickHandle.Reset();
        }
    };

    UdpClient->OnConnectionError = [this]()
    {
        OnConnectionError.Broadcast();

        if (TickHandle.IsValid())
        {
            FTSTicker::GetCoreTicker().RemoveTicker(TickHandle);
            TickHandle.Reset();
        }
    };

    UdpClient->OnDisconnect = [this]()
    {
        if (TickHandle.IsValid())
        {
            FTSTicker::GetCoreTicker().RemoveTicker(TickHandle);
            TickHandle.Reset();
        }
    };
}

void UENetSubsystem::Deinitialize()
{
    if (TickHandle.IsValid())
    {
        FTSTicker::GetCoreTicker().RemoveTicker(TickHandle);
        TickHandle.Reset();
    }

    if (UdpClient)
        UdpClient->Disconnect();

    UdpClient.Reset();
}

bool UENetSubsystem::Connect(const FString& Host, int32 Port)
{
    if (!UdpClient)
        UdpClient = MakeUnique<UDPClient>();

    return UdpClient->Connect(Host, Port);
}

void UENetSubsystem::Disconnect()
{
    if (UdpClient)
        UdpClient->Disconnect();
}

bool UENetSubsystem::Tick(float DeltaTime)
{
    return true;
}

void UENetSubsystem::SetConnectTimeout(float Seconds)
{
    if (UdpClient)
        UdpClient->SetConnectTimeout(Seconds);
}

void UENetSubsystem::SetRetryInterval(float Seconds)
{
    if (UdpClient)
        UdpClient->SetRetryInterval(Seconds);
}

void UENetSubsystem::SetRetryEnabled(bool bEnabled)
{
    if (UdpClient)
        UdpClient->SetRetryEnabled(bEnabled);
}

float UENetSubsystem::GetConnectTimeout() const
{
    return UdpClient ? UdpClient->GetConnectTimeout() : 3.0f;
}

float UENetSubsystem::GetRetryInterval() const
{
    return UdpClient ? UdpClient->GetRetryInterval() : 10.0f;
}

bool UENetSubsystem::IsRetryEnabled() const
{
    return UdpClient ? UdpClient->IsRetryEnabled() : false;
}

void UENetSubsystem::SendEntitySync(FVector Position, FRotator Rotation, int32 AnimID) const
{
    UFlatBuffer* syncBuffer = UFlatBuffer::CreateFlatBuffer(20);
    FSyncEntityPacket syncPacket = FSyncEntityPacket();
    syncPacket.Positon = Position;
    syncPacket.Rotator = Rotation;
    syncPacket.AnimationState = AnimID;
    syncPacket.Serialize(syncBuffer);

    if (UdpClient)
        UdpClient->Send(syncBuffer);
}

//%FUNCTIONS%
