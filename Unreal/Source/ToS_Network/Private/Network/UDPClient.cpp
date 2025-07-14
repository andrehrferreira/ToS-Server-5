#include "Network/UDPClient.h"
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

#include "Packets/PingPacket.h"

UDPClient::UDPClient() {}
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

                if (OnDataReceive)
                    OnDataReceive(Buffer);

                EPacketType PacketType = static_cast<EPacketType>(Buffer->ReadByte());

                switch(PacketType)
                {
                    case EPacketType::Ping:
                    {
                        uint16 PingTime = Buffer->ReadUInt16();
                        LastPingTime = FPlatformTime::Seconds();

                        UFlatBuffer* Buffer = UFlatBuffer::CreateFlatBuffer(3);
                        PingPacket pintPacket = PingPacket();
                        pintPacket.SentTimestamp = PingTime;
                        pintPacket.Serialize(Buffer);
                        
                        int32 BytesSent = 0;
                        Socket->SendTo(Buffer->GetRawBuffer(), Buffer->GetLength(), BytesSent, *RemoteEndpoint);

                        break;
                    }
                    case EPacketType::Reliable:
                    {
                        uint16 Seq = Buffer->ReadUInt16();
                        SendAck(Seq);
                        break;
                    }
                    case EPacketType::ConnectionAccepted:
                    {
                        StopRetryTimer();
                        bIsConnected = true;
                        bIsConnecting = false;
                        ConnectionStatus = EConnectionStatus::Connected;
                        TimeSinceConnect = 0.0f;
                        LastPingTime = FPlatformTime::Seconds();
                        {
                            int32 clientID = 0;

                            if (Buffer->GetLength() - Buffer->GetOffset() >= 4)
                                clientID = Buffer->ReadInt32();

                            if (OnConnect)
                                OnConnect(clientID);
                        }
                    }
                    break;
                    case EPacketType::ConnectionDenied:
                    {
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
                        uint16 IntegityKey = IntegrityTableData::GetKey(Index);

                        UFlatBuffer* Buffer = UFlatBuffer::CreateFlatBuffer(3);
                        Buffer->WriteByte(static_cast<uint8>(EPacketType::CheckIntegrity));
                        Buffer->WriteUInt16(Code);
                        int32 BytesSent = 0;
                        Socket->SendTo(Buffer->GetRawBuffer(), Buffer->GetLength(), BytesSent, *RemoteEndpoint);
					}
                    break;
                    default:
                        
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

    TArray<uint8> Packet;
    Packet.Add(static_cast<uint8>(EPacketType::Connect));
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
