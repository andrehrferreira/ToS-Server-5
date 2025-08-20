#include "Network/UDPClient.h"
#include "Utils/CRC32C.h"
#include "Sockets.h"
#include "SocketSubsystem.h"
#include "IPAddress.h"
#include "Common/UdpSocketBuilder.h"
#include "Network/UFlatBuffer.h"
#include "Network/IntegrityTable.h"
#include "Engine/World.h"
#include "TimerManager.h"
#include "HAL/Runnable.h"
#include "HAL/RunnableThread.h"
#include "HAL/ThreadSafeBool.h"
#include "Async/Async.h"

#include "Packets/PongPacket.h"
#include <sodium.h>
#include "Misc/Base64.h"
#include "Network/SecureSession.h"

UDPClient::UDPClient() : bCookieReceived(false) {}
UDPClient::~UDPClient() { Disconnect(); }

void UDPClient::StartRetryTimer()
{
    if (IsInGameThread())
    {
        if (GWorld)
        {
            GWorld->GetTimerManager().SetTimer(RetryTimerHandle, [this]() { this->OnRetryTimerTick(); }, 0.1f, true);
        }
    }
    else
    {
        AsyncTask(ENamedThreads::GameThread, [this]()
        {
            if (GWorld)
            {
                GWorld->GetTimerManager().SetTimer(RetryTimerHandle, [this]() { this->OnRetryTimerTick(); }, 0.1f, true);
            }
        });
    }
}

void UDPClient::StopRetryTimer()
{
    if (IsInGameThread())
    {
        if (GWorld && GWorld->GetTimerManager().IsTimerActive(RetryTimerHandle))
        {
            GWorld->GetTimerManager().ClearTimer(RetryTimerHandle);
        }
    }
    else
    {
        AsyncTask(ENamedThreads::GameThread, [this]()
        {
            if (GWorld && GWorld->GetTimerManager().IsTimerActive(RetryTimerHandle))
            {
                GWorld->GetTimerManager().ClearTimer(RetryTimerHandle);
            }
        });
    }
}

void UDPClient::StartPacketPollThread()
{
    StopPacketPollThread();
    PacketPollRunnable = new FPacketPollRunnable(this);
    PacketPollThread = FRunnableThread::Create(PacketPollRunnable, TEXT("UDPClientPacketPollThread"));
}

void UDPClient::StopPacketPollThread()
{
    if (PacketPollRunnable)
    {
        PacketPollRunnable->Stop();
    }

    if (PacketPollThread)
    {
        PacketPollThread->WaitForCompletion();
        delete PacketPollThread;
        PacketPollThread = nullptr;
    }

    if (PacketPollRunnable)
    {
        delete PacketPollRunnable;
        PacketPollRunnable = nullptr;
    }
}

void UDPClient::OnRetryTimerTick()
{
    if (bIsConnecting && !bIsConnected)
    {
        TimeSinceConnect += 0.1f;

        if (TimeSinceConnect >= ConnectTimeout)
        {
            bIsConnecting = false;
            ConnectionStatus = EConnectionStatus::ConnectionFailed;

            if (OnConnectionError)
                OnConnectionError();

            if (bRetryEnabled)
                TimeSinceLastRetry = 0.0f;
        }
    }

    if (bRetryEnabled && ConnectionStatus == EConnectionStatus::ConnectionFailed && !bIsConnected)
    {
        TimeSinceLastRetry += 0.1f;

        if (TimeSinceLastRetry >= RetryInterval && !LastHost.IsEmpty())
        {
            TimeSinceLastRetry = 0.0f;
            RetryCount++;
            Connect(LastHost, LastPort);
        }
    }
}

void UDPClient::SendAck(uint16 Sequence)
{
    if (Socket && RemoteEndpoint.IsValid())
    {
        UFlatBuffer* Buffer = UFlatBuffer::CreateFlatBuffer(3);
        Buffer->WriteByte(static_cast<uint8>(EPacketType::Ack));
        Buffer->WriteUInt16(Sequence);
        int32 BytesSent = 0;
        Socket->SendTo(Buffer->GetRawBuffer(), Buffer->GetLength(), BytesSent, *RemoteEndpoint);
    }
}

void UDPClient::Send(UFlatBuffer* buffer)
{
    if (!Socket || !RemoteEndpoint.IsValid())
        return;

    if (buffer->GetLength() <= 0)
        return;

    if (bEncryptionEnabled && bIsConnected)
    {
        SendEncrypted(buffer);
    }
    else
    {
        SendLegacy(buffer);
    }
}

void UDPClient::SendEncrypted(UFlatBuffer* buffer)
{
    TArray<uint8> Payload;
    Payload.SetNumUninitialized(buffer->GetLength() - 1);
    FMemory::Memcpy(Payload.GetData(), buffer->GetRawBuffer() + 1, buffer->GetLength() - 1);

    FPacketHeader Header;
    Header.ConnectionId = SecureSession.GetConnectionId();
    Header.Channel = EPacketChannel::Unreliable; 
    Header.Flags = EPacketHeaderFlags::Encrypted | EPacketHeaderFlags::AEAD_ChaCha20Poly1305;
    Header.Sequence = SecureSession.GetSeqTx();

    TArray<uint8> AAD = Header.GetAAD();
    TArray<uint8> Ciphertext;

    if (!SecureSession.EncryptPayload(Payload, AAD, Ciphertext))
    {
        UE_LOG(LogTemp, Error, TEXT("Failed to encrypt payload"));
        return;
    }

    TArray<uint8> FinalPacket;
    FinalPacket.SetNumUninitialized(FPacketHeader::Size + Ciphertext.Num());
    Header.Serialize(FinalPacket.GetData());
    FMemory::Memcpy(FinalPacket.GetData() + FPacketHeader::Size, Ciphertext.GetData(), Ciphertext.Num());

    uint32 Sign = FCRC32C::Compute(FinalPacket.GetData(), FinalPacket.Num());
    FinalPacket.Append(reinterpret_cast<uint8*>(&Sign), sizeof(uint32));

    int32 BytesSent = 0;
    Socket->SendTo(FinalPacket.GetData(), FinalPacket.Num(), BytesSent, *RemoteEndpoint);
}

void UDPClient::SendLegacy(UFlatBuffer* buffer)
{
    int len = buffer->GetLength();
    uint32 sign = FCRC32C::Compute(buffer->GetRawBuffer(), len);
    buffer->Write<uint32>(sign);
    int32 BytesSent = 0;
    Socket->SendTo(buffer->GetRawBuffer(), buffer->GetLength(), BytesSent, *RemoteEndpoint);
}

void UDPClient::PollIncomingPackets()
{
    if (!Socket || (!bIsConnected && !bIsConnecting))
        return;

    if (bIsConnected && (FPlatformTime::Seconds() - LastPingTime > 15.0))
    {
        if (OnDisconnect)
            OnDisconnect();

        Disconnect();
        return;
    }

    if (!Socket || (!bIsConnected && !bIsConnecting))
        return;

    uint32 PendingDataSize = 0;
    while (!PacketPollRunnable->bStop && Socket->HasPendingData(PendingDataSize))
    {
        TArray<uint8> ReceivedData;
        ReceivedData.SetNumUninitialized(PendingDataSize);
        int32 BytesRead = 0;
        TSharedRef<FInternetAddr> Sender = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();

        if (Socket->RecvFrom(ReceivedData.GetData(), ReceivedData.Num(), BytesRead, *Sender))
        {
            if (BytesRead > 0)
            {
                UFlatBuffer* Buffer = UFlatBuffer::CreateFlatBuffer(BytesRead);
                Buffer->CopyFromMemory(ReceivedData.GetData(), BytesRead);
                Buffer->PrintBuffer(Buffer->GetRawBuffer(), BytesRead);

                EPacketType PacketType = static_cast<EPacketType>(Buffer->ReadByte());
                //FString PacketName = StaticEnum<EPacketType>()->GetNameStringByValue(static_cast<int64>(PacketType));
                //UE_LOG(LogTemp, Warning, TEXT("UDPClient: Received %s (%d bytes)."), *PacketName, BytesRead);

                switch(PacketType)
                {
                    case EPacketType::Ping:
                    {
                        uint16 PingTime = Buffer->ReadUInt16();
                        LastPingTime = FPlatformTime::Seconds();

                        UFlatBuffer* PongBuffer = UFlatBuffer::CreateFlatBuffer(3);
                        FPongPacket pongPacket = FPongPacket();
                        pongPacket.SentTimestamp = PingTime;
                        pongPacket.Serialize(PongBuffer);

                        int32 BytesSent = 0;
                        Socket->SendTo(PongBuffer->GetRawBuffer(), PongBuffer->GetLength(), BytesSent, *RemoteEndpoint);
                    }
                    break;
                    case EPacketType::Unreliable:
                    {
                        if (OnDataReceive)
                            OnDataReceive(Buffer);
                    }
                    break;
                    case EPacketType::Reliable:
                    {
                        //uint16 Seq = Buffer->ReadUInt16();
                        //SendAck(Seq);

                        if (OnDataReceive)
                            OnDataReceive(Buffer);
                    }
                    break;
                    case EPacketType::ConnectionAccepted:
                    {
                        StopRetryTimer();
                        bIsConnected = true;
                        bIsConnecting = false;
                        ConnectionStatus = EConnectionStatus::Connected;
                        TimeSinceConnect = 0.0f;
                        LastPingTime = FPlatformTime::Seconds();
                        {
                            uint32 connectionID = Buffer->ReadUInt32();

                            ServerPublicKey.SetNumUninitialized(32);
                            for (int32 i = 0; i < 32; ++i)
                                ServerPublicKey[i] = Buffer->ReadByte();

                            Salt.SetNumUninitialized(16);
                            for (int32 i = 0; i < 16; ++i)
                                Salt[i] = Buffer->ReadByte();

                            // Initialize secure session
                            if (SecureSession.InitializeAsClient(ClientPrivateKey, ServerPublicKey, Salt, connectionID))
                            {
                                bEncryptionEnabled = true;

                                if (OnConnect)
                                    OnConnect(connectionID);
                            }
                            else
                            {
                                UE_LOG(LogTemp, Error, TEXT("Failed to initialize secure session"));
                                Disconnect();
                            }
                        }
                    }
                    break;
                    case EPacketType::Cookie:
                    {
                        if (BytesRead == 1 + 48 && !bCookieReceived)
                        {
                            ServerCookie.SetNumUninitialized(48);
                            for (int32 i = 0; i < 48; ++i)
                                ServerCookie[i] = Buffer->ReadByte();

                            bCookieReceived = true;
                            TArray<uint8> ConnectWithCookie;
                            ConnectWithCookie.Add(static_cast<uint8>(EPacketType::Connect));
                            ConnectWithCookie.Append(ClientPublicKey.GetData(), ClientPublicKey.Num());
                            ConnectWithCookie.Append(ServerCookie.GetData(), ServerCookie.Num());

                            int32 BytesSent = 0;
                            Socket->SendTo(ConnectWithCookie.GetData(), ConnectWithCookie.Num(), BytesSent, *RemoteEndpoint);
                        }
                    }
                    break;
                    case EPacketType::ConnectionDenied:
                    {
                        // Actual connection denied
                        bIsConnected = false;
                        bIsConnecting = false;
                        ConnectionStatus = EConnectionStatus::ConnectionFailed;

                        if (OnConnectDenied)
                            OnConnectDenied();

                        StopPacketPollThread();
                        StartRetryTimer();
                    }
                    break;
                    case EPacketType::Disconnect:
                    {
                        Disconnect();

                        if (OnDisconnect)
                            OnDisconnect();

                        StopPacketPollThread();
                    }
                    break;
                    case EPacketType::CheckIntegrity:
                    {
                        uint16 Index = Buffer->ReadUInt16();
                        uint16 IntegrityKey = IntegrityTableData::GetKey(Index);

                        UFlatBuffer* ResponseBuffer = UFlatBuffer::CreateFlatBuffer(3);
                        ResponseBuffer->WriteByte(static_cast<uint8>(EPacketType::CheckIntegrity));
                        ResponseBuffer->WriteUInt16(IntegrityKey);
                        int32 BytesSent = 0;
                        Socket->SendTo(ResponseBuffer->GetRawBuffer(), ResponseBuffer->GetLength(), BytesSent, *RemoteEndpoint);
                    }
                    break;
                }
            }
        }
    }
}

bool UDPClient::Connect(const FString& Host, int32 Port)
{
    if (bIsConnected || bIsConnecting)
        return false;

    Disconnect();
    bIsConnecting = true;
    ConnectionStatus = EConnectionStatus::Connecting;
    TimeSinceConnect = 0.0f;
    TimeSinceLastRetry = 0.0f;
    LastHost = Host;
    LastPort = Port;
    RetryCount = 0;
    StartRetryTimer();
    StartPacketPollThread();
    LastPingTime = FPlatformTime::Seconds();

    ISocketSubsystem* SocketSubsystem = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM);
    bool bIsValid;
    RemoteEndpoint = TSharedPtr<FInternetAddr>(SocketSubsystem->CreateInternetAddr());
    RemoteEndpoint->SetIp(*Host, bIsValid);
    RemoteEndpoint->SetPort(Port);

    if (!bIsValid)
    {
        bIsConnected = false;
        bIsConnecting = false;
        ConnectionStatus = EConnectionStatus::ConnectionFailed;

        if (OnConnectionError)
            OnConnectionError();

        return false;
    }

    Socket = FUdpSocketBuilder(TEXT("UDPClientSocket"))
        .AsNonBlocking()
        .AsReusable()
        .WithReceiveBufferSize(2 * 1024 * 1024);

    if (!Socket)
    {
        bIsConnected = false;
        bIsConnecting = false;
        ConnectionStatus = EConnectionStatus::ConnectionFailed;

        if (OnConnectionError)
            OnConnectionError();

        return false;
    }

    if (sodium_init() < 0)
    {
        bIsConnected = false;
        bIsConnecting = false;
        ConnectionStatus = EConnectionStatus::ConnectionFailed;

        if (OnConnectionError)
            OnConnectionError();

        return false;
    }

    ClientPublicKey.SetNumUninitialized(32);
    ClientPrivateKey.SetNumUninitialized(32);
    crypto_box_keypair(ClientPublicKey.GetData(), ClientPrivateKey.GetData());

    // Send initial connect packet (no cookie yet)
    TArray<uint8> Packet;
    Packet.Add(static_cast<uint8>(EPacketType::Connect));
    Packet.Append(ClientPublicKey.GetData(), ClientPublicKey.Num());
    int32 BytesSent = 0;

    if (Socket->SendTo(Packet.GetData(), Packet.Num(), BytesSent, *RemoteEndpoint))
    {
        bIsConnected = false;
        bIsConnecting = true;
        ConnectionStatus = EConnectionStatus::Connecting;
    }
    else
    {
        bIsConnected = false;
        bIsConnecting = false;
        ConnectionStatus = EConnectionStatus::ConnectionFailed;

        if (OnConnectionError)
            OnConnectionError();

        return false;
    }

    return true;
}

void UDPClient::Disconnect()
{
    if (Socket)
    {
        Socket->Close();
        ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->DestroySocket(Socket);
        Socket = nullptr;
        bIsConnected = false;
        bIsConnecting = false;
        ConnectionStatus = EConnectionStatus::Disconnected;
    }
    else
    {
        bIsConnected = false;
        bIsConnecting = false;
        ConnectionStatus = EConnectionStatus::Disconnected;
    }

    TimeSinceConnect = 0.0f;
    TimeSinceLastRetry = 0.0f;
    RetryCount = 0;
    StopRetryTimer();
    StopPacketPollThread();
}

uint32 FPacketPollRunnable::Run()
{
    while (!bStop)
    {
        if (Client)
            Client->PollIncomingPackets();

        FPlatformProcess::Sleep(0.001f);
    }

    return 0;
}
