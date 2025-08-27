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
#include "Packets/BenchmarkPacket.h"
#include "Packets/CreateEntityPacket.h"
#include "Packets/UpdateEntityPacket.h"
#include "Packets/RemoveEntityPacket.h"
#include "Packets/UpdateEntityQuantizedPacket.h"
#include "Packets/RekeyRequestPacket.h"
#include "Packets/DeltaSyncPacket.h"
#include "Packets/SyncEntityPacket.h"
#include "Packets/SyncEntityQuantizedPacket.h"
#include "Packets/EnterToWorldPacket.h"
#include "Packets/RekeyResponsePacket.h"

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
                        case EServerPackets::UpdateEntityQuantized:
                        {
                            FUpdateEntityQuantizedPacket fUpdateEntityQuantized = FUpdateEntityQuantizedPacket();
                            fUpdateEntityQuantized.Deserialize(Buffer);
                            static int32 QuantizedUpdateCount = 0;
                            QuantizedUpdateCount++;
                            
                            if (QuantizedUpdateCount <= 10)
                            {
                                ClientFileLog(FString::Printf(TEXT("=== RECEIVED UpdateEntityQuantizedPacket #%d ==="), QuantizedUpdateCount));
                                ClientFileLog(FString::Printf(TEXT("[CLIENT] EntityId: %d"), fUpdateEntityQuantized.EntityId));
                                ClientFileLog(FString::Printf(TEXT("[CLIENT] Quantized Position: (%d, %d, %d)"), fUpdateEntityQuantized.QuantizedX, fUpdateEntityQuantized.QuantizedY, fUpdateEntityQuantized.QuantizedZ));
                                ClientFileLog(FString::Printf(TEXT("[CLIENT] Quadrant: (%d, %d)"), fUpdateEntityQuantized.QuadrantX, fUpdateEntityQuantized.QuadrantY));
                                ClientFileLog(FString::Printf(TEXT("[CLIENT] Yaw: %f"), fUpdateEntityQuantized.Yaw));
                                ClientFileLog(FString::Printf(TEXT("[CLIENT] Velocity: %s"), *fUpdateEntityQuantized.Velocity.ToString()));
                                ClientFileLog(FString::Printf(TEXT("[CLIENT] AnimationState: %d"), fUpdateEntityQuantized.AnimationState));
                                ClientFileLog(FString::Printf(TEXT("[CLIENT] Flags: %d"), fUpdateEntityQuantized.Flags));
                            }
                            
                            OnUpdateEntityQuantized.Broadcast(fUpdateEntityQuantized);
                            
                            if (QuantizedUpdateCount <= 10)
                            {
                                ClientFileLog(TEXT("[CLIENT] UpdateEntityQuantized broadcasted to delegates"));
                            }
                        }
                        break;
                        case EServerPackets::RekeyRequest:
                        {
                            FRekeyRequestPacket fRekeyRequest = FRekeyRequestPacket();
                            fRekeyRequest.Deserialize(Buffer);
                            OnRekeyRequest.Broadcast(fRekeyRequest.CurrentSequence, fRekeyRequest.NewSalt);
                        }
                        break;

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

void UENetSubsystem::SendEntitySyncQuantized(FVector Position, FRotator Rotation, int32 AnimID, FVector Velocity, bool IsFalling) const
{
    static int32 SyncCount = 0;
    SyncCount++;

    if (SyncCount <= 10)
    {
        ClientFileLog(FString::Printf(TEXT("=== SENDING SyncEntityQuantizedPacket #%d ==="), SyncCount));
        ClientFileLog(FString::Printf(TEXT("[CLIENT SEND] 🚀 Original Position: %s"), *Position.ToString()));
        ClientFileLog(FString::Printf(TEXT("[CLIENT SEND] 🚀 Original Rotation: %s"), *Rotation.ToString()));
        ClientFileLog(FString::Printf(TEXT("[CLIENT SEND] 🚀 Original Velocity: %s"), *Velocity.ToString()));
        ClientFileLog(FString::Printf(TEXT("[CLIENT SEND] 🚀 AnimID: %d, IsFalling: %s"), AnimID, IsFalling ? TEXT("true") : TEXT("false")));
    }

    // World Origin Rebasing quantization
    const float QuadrantSize = 25600.0f * 4; // 102400 units per quadrant
    int32 QuadrantX = FMath::FloorToInt(Position.X / QuadrantSize);
    int32 QuadrantY = FMath::FloorToInt(Position.Y / QuadrantSize);

    // Calculate relative position within quadrant
    FVector RelativePosition = FVector(
        Position.X - (QuadrantX * QuadrantSize),
        Position.Y - (QuadrantY * QuadrantSize),
        Position.Z
    );

    // Quantize position with scale factor
    const float Scale = 100.0f;
    int16 QuantizedX = FMath::Clamp(FMath::RoundToInt(RelativePosition.X / Scale), -32768, 32767);
    int16 QuantizedY = FMath::Clamp(FMath::RoundToInt(RelativePosition.Y / Scale), -32768, 32767);
    int16 QuantizedZ = FMath::Clamp(FMath::RoundToInt(RelativePosition.Z / Scale), -32768, 32767);

    // Use only Yaw for rotation optimization
    float Yaw = Rotation.Yaw;

    if (SyncCount <= 10)
    {
        ClientFileLog(FString::Printf(TEXT("[CLIENT SEND] 📦 Quadrant: X=%d Y=%d"), QuadrantX, QuadrantY));
        ClientFileLog(FString::Printf(TEXT("[CLIENT SEND] 📦 Quantized: X=%d Y=%d Z=%d"), QuantizedX, QuantizedY, QuantizedZ));
        ClientFileLog(FString::Printf(TEXT("[CLIENT SEND] 📦 Relative Position: %s"), *RelativePosition.ToString()));
        ClientFileLog(FString::Printf(TEXT("[CLIENT SEND] 📦 Yaw Only: %f"), Yaw));
    }

    UFlatBuffer* syncBuffer = UFlatBuffer::CreateFlatBuffer(26); // Quantized SyncEntity packet size
    FSyncEntityQuantizedPacket syncPacket = FSyncEntityQuantizedPacket();
    syncPacket.QuantizedX = QuantizedX;
    syncPacket.QuantizedY = QuantizedY;
    syncPacket.QuantizedZ = QuantizedZ;
    syncPacket.QuadrantX = static_cast<int16>(QuadrantX);
    syncPacket.QuadrantY = static_cast<int16>(QuadrantY);
    syncPacket.Yaw = Yaw;
    syncPacket.Velocity = Velocity;
    syncPacket.AnimationState = static_cast<uint16>(AnimID);
    syncPacket.IsFalling = IsFalling;

    if (SyncCount <= 10)
    {
        ClientFileLog(TEXT("=== CLIENT SERIALIZATION DEBUG ==="));
        ClientFileLog(FString::Printf(TEXT("[CLIENT STRUCT] QuantizedX: %d"), syncPacket.QuantizedX));
        ClientFileLog(FString::Printf(TEXT("[CLIENT STRUCT] QuantizedY: %d"), syncPacket.QuantizedY));
        ClientFileLog(FString::Printf(TEXT("[CLIENT STRUCT] QuantizedZ: %d"), syncPacket.QuantizedZ));
        ClientFileLog(FString::Printf(TEXT("[CLIENT STRUCT] QuadrantX: %d"), syncPacket.QuadrantX));
        ClientFileLog(FString::Printf(TEXT("[CLIENT STRUCT] QuadrantY: %d"), syncPacket.QuadrantY));
        ClientFileLog(FString::Printf(TEXT("[CLIENT STRUCT] Yaw: %f"), syncPacket.Yaw));
        ClientFileLog(FString::Printf(TEXT("[CLIENT STRUCT] Velocity: %s"), *syncPacket.Velocity.ToString()));
        ClientFileLog(FString::Printf(TEXT("[CLIENT STRUCT] AnimationState: %d"), syncPacket.AnimationState));
        ClientFileLog(FString::Printf(TEXT("[CLIENT STRUCT] IsFalling: %s"), syncPacket.IsFalling ? TEXT("true") : TEXT("false")));
        ClientFileLog(TEXT("[CLIENT] About to serialize SyncEntityQuantizedPacket..."));
    }

    syncPacket.Serialize(syncBuffer);

    if (SyncCount <= 10 && syncBuffer)
    {
        ClientFileLog(FString::Printf(TEXT("[CLIENT] ✅ Serialization complete - buffer length: %d"), syncBuffer->GetLength()));

        if (syncBuffer->GetLength() > 0)
        {
            // Log all bytes in the buffer for detailed analysis
            TArray<uint8> BufferData;
            BufferData.SetNumUninitialized(FMath::Min(syncBuffer->GetLength(), 26)); // Full packet
            FMemory::Memcpy(BufferData.GetData(), syncBuffer->GetRawBuffer(), BufferData.Num());

            ClientFileLog(TEXT("=== SERIALIZED BUFFER ANALYSIS ==="));
            for (int32 i = 0; i < BufferData.Num(); i++)
            {
                FString ByteDescription;
                if (i == 0) ByteDescription = TEXT("PacketType");
                else if (i == 1) ByteDescription = TEXT("ClientPacket Low");
                else if (i == 2) ByteDescription = TEXT("ClientPacket High");
                else if (i >= 3 && i <= 4) ByteDescription = FString::Printf(TEXT("QuantizedX[%d]"), i-3);
                else if (i >= 5 && i <= 6) ByteDescription = FString::Printf(TEXT("QuantizedY[%d]"), i-5);
                else if (i >= 7 && i <= 8) ByteDescription = FString::Printf(TEXT("QuantizedZ[%d]"), i-7);
                else if (i >= 9 && i <= 10) ByteDescription = FString::Printf(TEXT("QuadrantX[%d]"), i-9);
                else if (i >= 11 && i <= 12) ByteDescription = FString::Printf(TEXT("QuadrantY[%d]"), i-11);
                else if (i >= 13 && i <= 16) ByteDescription = FString::Printf(TEXT("Yaw[%d]"), i-13);
                else ByteDescription = FString::Printf(TEXT("Data[%d]"), i);

                ClientFileLog(FString::Printf(TEXT("[CLIENT BUFFER] [%02d] = %3d (0x%02X) - %s"),
                    i, BufferData[i], BufferData[i], *ByteDescription));
            }

            // Interpret specific fields from buffer (manual)
            if (BufferData.Num() >= 10)
            {
                int16 SerializedQuantizedX = (int16)((BufferData[4] << 8) | BufferData[3]);
                int16 SerializedQuantizedY = (int16)((BufferData[6] << 8) | BufferData[5]);
                int16 SerializedQuantizedZ = (int16)((BufferData[8] << 8) | BufferData[7]);
                int16 SerializedQuadrantX = (int16)((BufferData[10] << 8) | BufferData[9]);
                int16 SerializedQuadrantY = (int16)((BufferData[12] << 8) | BufferData[11]);

                ClientFileLog(TEXT("=== MANUAL BUFFER INTERPRETATION ==="));
                ClientFileLog(FString::Printf(TEXT("[CLIENT MANUAL] QuantizedX: %d (from bytes %02X %02X)"), SerializedQuantizedX, BufferData[3], BufferData[4]));
                ClientFileLog(FString::Printf(TEXT("[CLIENT MANUAL] QuantizedY: %d (from bytes %02X %02X)"), SerializedQuantizedY, BufferData[5], BufferData[6]));
                ClientFileLog(FString::Printf(TEXT("[CLIENT MANUAL] QuantizedZ: %d (from bytes %02X %02X)"), SerializedQuantizedZ, BufferData[7], BufferData[8]));
                ClientFileLog(FString::Printf(TEXT("[CLIENT MANUAL] QuadrantX: %d (from bytes %02X %02X)"), SerializedQuadrantX, BufferData[9], BufferData[10]));
                ClientFileLog(FString::Printf(TEXT("[CLIENT MANUAL] QuadrantY: %d (from bytes %02X %02X)"), SerializedQuadrantY, BufferData[11], BufferData[12]));
            }
        }
    }

    if (UdpClient)
    {
        if (SyncCount <= 5)
        {
            ClientFileLog(TEXT("[CLIENT] Sending SyncEntityQuantizedPacket via UdpClient..."));
        }
        UdpClient->Send(syncBuffer);
    }
    else
    {
        ClientFileLog(TEXT("[CLIENT] ERROR: UdpClient is NULL!"));
    }
}


