#include "Network/ENetSubsystem.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "Utils/CRC32C.h"
#include "Utils/FileLogger.h"
#include "Misc/ScopeLock.h"
#include "Sockets.h"
#include "SocketSubsystem.h"
#include "IPAddress.h"
#include "Common/UdpSocketBuilder.h"
//%INCLUDES%
#include "Enum/EntityDelta.h"

void UENetSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
    UdpClient = MakeUnique<UDPClient>();

    UdpClient->OnDataReceive = [this](UFlatBuffer* Buffer)
    {
        if (Buffer && Buffer->Position == 1)
        {
            auto bufferSign = Buffer->ReadSign();
            auto sign = FCRC32C::Compute(Buffer->Data, Buffer->Capacity);

            if (bufferSign == sign) {
                do {
                    if(Buffer->Position > 2)
                        EPacketType PacketType = static_cast<EPacketType>(Buffer->ReadByte());

                    EServerPackets ServerPacketType = static_cast<EServerPackets>(Buffer->ReadInt16());
                    //FString PacketName = StaticEnum<EServerPackets>()->GetNameStringByValue(static_cast<int64>(ServerPacketType));
                    //UE_LOG(LogTemp, Warning, TEXT("UENetSubsystem: Received %s."), *PacketName);

                    switch (ServerPacketType) {
//%DATASWITCH%
                        case EServerPackets::DeltaSync:
                        {
                            FDeltaSyncPacket delta = FDeltaSyncPacket();
                            delta.Deserialize(Buffer);

                            FDeltaUpdateData data;
                            data.Index = delta.Index;
                            data.EntitiesMask = static_cast<EEntityDelta>(delta.EntitiesMask);
                            data.Velocity = Buffer->Read<FVector>();
                            data.Flags = Buffer->Read<uint32>();

                            if (EnumHasAnyFlags(data.EntitiesMask, EEntityDelta::Position))
                                data.Positon = Buffer->Read<FVector>();
                            if (EnumHasAnyFlags(data.EntitiesMask, EEntityDelta::Rotation))
                                data.Rotator = Buffer->Read<FRotator>();
                            if (EnumHasAnyFlags(data.EntitiesMask, EEntityDelta::AnimState))
                                data.AnimationState = static_cast<int32>(Buffer->Read<uint32>());

                            OnDeltaSync.Broadcast(data.Index, static_cast<uint8>(data.EntitiesMask));
                            OnDeltaUpdate.Broadcast(data);
                        }
                        break;
                    }
                } while (Buffer->Position < Buffer->Capacity);
            }
            else {
                UE_LOG(LogTemp, Warning, TEXT("UENetSubsystem: Sign %d / %d."), bufferSign, sign);
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

void UENetSubsystem::SendEntitySync(FVector Position, FRotator Rotation, int32 AnimID, FVector Velocity, bool IsFalling) const
{
    static int32 SyncCount = 0;
    SyncCount++;

    if (SyncCount <= 5)
    {
        ClientFileLog(FString::Printf(TEXT("=== SENDING SyncEntityPacket #%d ==="), SyncCount));
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Position: %s"), *Position.ToString()));
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Rotation: %s"), *Rotation.ToString()));
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Velocity: %s"), *Velocity.ToString()));
        ClientFileLog(FString::Printf(TEXT("[CLIENT] AnimID: %d, IsFalling: %s"), AnimID, IsFalling ? TEXT("true") : TEXT("false")));
    }

    UFlatBuffer* syncBuffer = UFlatBuffer::CreateFlatBuffer(42); // Full SyncEntity packet size
    FSyncEntityPacket syncPacket = FSyncEntityPacket();
    syncPacket.Positon = Position;
    syncPacket.Rotator = Rotation;
    syncPacket.AnimationState = AnimID;
    syncPacket.Velocity = Velocity;
    syncPacket.IsFalling = IsFalling;

    if (SyncCount <= 5)
    {
        ClientFileLog(TEXT("[CLIENT] About to serialize SyncEntityPacket..."));
    }

    if (SyncCount <= 5)
    {
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Before serialization - Buffer capacity: %d"), syncBuffer->GetCapacity()));
    }

    syncPacket.Serialize(syncBuffer);

    if (SyncCount <= 5 && syncBuffer)
    {
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Buffer created with length: %d"), syncBuffer->GetLength()));
        if (syncBuffer->GetLength() > 0)
        {
            TArray<uint8> BufferData;
            BufferData.SetNumUninitialized(FMath::Min(syncBuffer->GetLength(), 10));
            FMemory::Memcpy(BufferData.GetData(), syncBuffer->GetRawBuffer(), BufferData.Num());

            FString HexData;
            for (int32 i = 0; i < BufferData.Num(); i++)
            {
                HexData += FString::Printf(TEXT("%02X"), BufferData[i]);
            }
            ClientFileLog(FString::Printf(TEXT("[CLIENT] First %d bytes: %s"), BufferData.Num(), *HexData));
            ClientFileLog(FString::Printf(TEXT("[CLIENT] First byte (should be 4 for Unreliable): %d"), BufferData[0]));
            if (BufferData.Num() >= 3)
            {
                ClientFileLog(FString::Printf(TEXT("[CLIENT] Bytes 2-3 (should be 0,0 for SyncEntity): %d, %d"), BufferData[1], BufferData[2]));
            }
        }
    }

    if (UdpClient)
    {
        if (SyncCount <= 5)
        {
            ClientFileLog(TEXT("[CLIENT] Sending SyncEntityPacket via UdpClient..."));
        }
        UdpClient->Send(syncBuffer);
    }
    else
    {
        ClientFileLog(TEXT("[CLIENT] ERROR: UdpClient is NULL!"));
    }
}

//%FUNCTIONS%
