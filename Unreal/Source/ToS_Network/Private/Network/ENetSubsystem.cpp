#include "Network/ENetSubsystem.h"
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "Network/ServerPackets.h"
#include "Utils/CRC32C.h"
#include "Misc/ScopeLock.h"
#include "Sockets.h"
#include "SocketSubsystem.h"
#include "IPAddress.h"
#include "Common/UdpSocketBuilder.h"
#include "Packets/BenchmarkPacket.h"
#include "Packets/CreateEntityPacket.h"
#include "Packets/UpdateEntityPacket.h"
#include "Packets/RemoveEntityPacket.h"
#include "Packets/DeltaSyncPacket.h"
#include "Packets/SyncEntityPacket.h"
#include "Packets/PongPacket.h"
#include "Packets/EnterToWorldPacket.h"
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
                    EServerPackets ServerPacketType = static_cast<EServerPackets>(Buffer->ReadInt16());
                    //FString PacketName = StaticEnum<EServerPackets>()->GetNameStringByValue(static_cast<int64>(ServerPacketType));
                    //UE_LOG(LogTemp, Warning, TEXT("UENetSubsystem: Received %s."), *PacketName);

                    switch (ServerPacketType) {
                        case EServerPackets::CreateEntity:
                        {
                            FCreateEntityPacket fCreateEntity = FCreateEntityPacket();
                            fCreateEntity.Deserialize(Buffer);
                            OnCreateEntity.Broadcast(fCreateEntity.EntityId, fCreateEntity.Positon, fCreateEntity.Rotator, fCreateEntity.Flags);
                        }
                        break;
                        case EServerPackets::UpdateEntity:
                        {
                            FUpdateEntityPacket fUpdateEntity = FUpdateEntityPacket();
                            fUpdateEntity.Deserialize(Buffer);
                            OnUpdateEntity.Broadcast(fUpdateEntity);
                        }
                        break;
                        case EServerPackets::RemoveEntity:
                        {
                            FRemoveEntityPacket fRemoveEntity = FRemoveEntityPacket();
                            fRemoveEntity.Deserialize(Buffer);
                            OnRemoveEntity.Broadcast(fRemoveEntity.EntityId);
                        }
                        break;

                        case EServerPackets::DeltaSync:
                        {
                            FDeltaSyncPacket delta = FDeltaSyncPacket();
                            delta.Deserialize(Buffer);

                            FDeltaUpdateData data;
                            data.Index = delta.Index;
                            data.EntitiesMask = static_cast<EEntityDelta>(delta.EntitiesMask);

                            if (EnumHasAnyFlags(data.EntitiesMask, EEntityDelta::Position))
                                data.Positon = Buffer->Read<FVector>();
                            if (EnumHasAnyFlags(data.EntitiesMask, EEntityDelta::Rotation))
                                data.Rotator = Buffer->Read<FRotator>();
                            if (EnumHasAnyFlags(data.EntitiesMask, EEntityDelta::AnimState))
                                data.AnimationState = static_cast<int32>(Buffer->Read<uint32>());
                            if (EnumHasAnyFlags(data.EntitiesMask, EEntityDelta::Velocity))
                                data.Velocity = Buffer->Read<FVector>();
                            if (EnumHasAnyFlags(data.EntitiesMask, EEntityDelta::Flags))
                                data.Flags = Buffer->Read<uint32>();

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
    UFlatBuffer* syncBuffer = UFlatBuffer::CreateFlatBuffer(34);
    FSyncEntityPacket syncPacket = FSyncEntityPacket();
    syncPacket.Positon = Position;
    syncPacket.Rotator = Rotation;
    syncPacket.AnimationState = AnimID;
    syncPacket.Velocity = Velocity;
    syncPacket.IsFalling = IsFalling;
    syncPacket.Serialize(syncBuffer);

    if (UdpClient)
        UdpClient->Send(syncBuffer);
}


