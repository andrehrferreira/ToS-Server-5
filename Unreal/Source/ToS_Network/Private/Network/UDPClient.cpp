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
#include "Utils/FileLogger.h"

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

    if (IsCryptoReady())
    {
        SendEncrypted(buffer, false);
    }
    else
    {
        SendLegacy(buffer);
    }
}

void UDPClient::SendEncrypted(UFlatBuffer* buffer, bool reliable)
{
    TArray<uint8> Payload;
    Payload.SetNumUninitialized(buffer->GetLength());
    FMemory::Memcpy(Payload.GetData(), buffer->GetRawBuffer(), buffer->GetLength());

    FPacketHeader Header;
    Header.ConnectionId = SecureSession.GetConnectionId();
    Header.Channel = reliable ? EPacketChannel::ReliableOrdered : EPacketChannel::Unreliable;
    Header.Flags = EPacketHeaderFlags::Encrypted | EPacketHeaderFlags::AEAD_ChaCha20Poly1305;
    Header.Sequence = SecureSession.GetSeqTx();

    TArray<uint8> AAD = Header.GetAAD();
    TArray<uint8> Result;
    bool bWasCompressed = false;

    if (Header.Sequence == 0)
    {
        ClientFileLog(FString::Printf(TEXT("=== ENCRYPTING RELIABLE HANDSHAKE PACKET ===")));
        ClientFileLog(FString::Printf(TEXT("[CLIENT] ConnectionId: %u"), Header.ConnectionId));
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Sequence: %llu"), (unsigned long long)Header.Sequence));
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Channel: %d"), (int32)Header.Channel));
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Flags: %d"), (int32)Header.Flags));
        ClientFileLogHex(TEXT("[CLIENT] AAD"), AAD);
        ClientFileLogHex(TEXT("[CLIENT] Payload"), Payload);
    }

    if (Header.Channel == EPacketChannel::Unreliable && Header.Sequence > 0 && Header.Sequence <= 10)
    {
        FString PacketTypeName = TEXT("Unknown");
        if (Payload.Num() > 0)
        {
            uint8 FirstByte = Payload[0];
            switch ((EPacketType)FirstByte)
            {
                case EPacketType::Connect: PacketTypeName = TEXT("Connect"); break;
                case EPacketType::Ping: PacketTypeName = TEXT("Ping"); break;
                case EPacketType::Pong: PacketTypeName = TEXT("Pong"); break;
                case EPacketType::BenckmarkTest: PacketTypeName = TEXT("Benchmark Test"); break;
                case EPacketType::ReliableHandshake: PacketTypeName = TEXT("Reliable Handshake"); break;
                case EPacketType::Disconnect: PacketTypeName = TEXT("Disconnect"); break;
                default: PacketTypeName = FString::Printf(TEXT("Unknown Type %d"), FirstByte); break;
            }
        }

        ClientFileLog(FString::Printf(TEXT("=== SENDING %s (seq=%llu) ==="), *PacketTypeName, (unsigned long long)Header.Sequence));
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Channel: %s"), (Header.Channel == EPacketChannel::Unreliable) ? TEXT("Unreliable") : TEXT("Reliable")));
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Payload Size: %d bytes"), Payload.Num()));
        ClientFileLogHex(TEXT("[CLIENT] RAW Payload"), Payload);

        ClientFileLog(TEXT("[CLIENT] === DETAILED PAYLOAD ANALYSIS ==="));
        for (int32 i = 0; i < FMath::Min(Payload.Num(), 24); i++)
        {
            FString ByteDescription;
            if (i == 0) ByteDescription = TEXT(" (Should be EPacketType)");
            else if (i == 1) ByteDescription = TEXT(" (ClientPacket low byte)");
            else if (i == 2) ByteDescription = TEXT(" (ClientPacket high byte)");
            else if (i >= 3 && i <= 14) ByteDescription = FString::Printf(TEXT(" (Position data byte %d)"), i - 2);
            else if (i >= 15 && i <= 26) ByteDescription = FString::Printf(TEXT(" (Rotation data byte %d)"), i - 14);

            ClientFileLog(FString::Printf(TEXT("[CLIENT] Payload[%02d] = %3d (0x%02X)%s"), i, Payload[i], Payload[i], *ByteDescription));
        }

        if (Payload.Num() >= 3)
        {
            uint16 ClientPacketValue = (uint16)Payload[1] | ((uint16)Payload[2] << 8);
            ClientFileLog(FString::Printf(TEXT("[CLIENT] ClientPacket interpreted as uint16: %d"), ClientPacketValue));
            ClientFileLog(FString::Printf(TEXT("[CLIENT] Should be 0 for SyncEntity: %s"), (ClientPacketValue == 0) ? TEXT("YES") : TEXT("NO")));
        }
    }

    if (!SecureSession.EncryptPayloadWithCompression(Payload, AAD, Result, bWasCompressed))
    {
        UE_LOG(LogTemp, Error, TEXT("Failed to encrypt payload"));
        return;
    }

    if (bWasCompressed)
        Header.Flags |= EPacketHeaderFlags::Compressed;

    if (Header.Sequence == 0)
    {
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Compression: %s"), bWasCompressed ? TEXT("true") : TEXT("false")));
        ClientFileLogHex(TEXT("[CLIENT] Ciphertext"), Result);
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Ciphertext Length: %d bytes"), Result.Num()));

        FString TxKeyHex;
        for (int32 i = 0; i < 32; i++)
        {
            TxKeyHex += FString::Printf(TEXT("%02X"), SecureSession.GetTxKey()[i]);
        }

        ClientFileLog(FString::Printf(TEXT("[CLIENT] TxKey: %s"), *TxKeyHex));
    }

    TArray<uint8> FinalPacket;
    FinalPacket.SetNumUninitialized(FPacketHeader::Size + Result.Num());

    Header.Serialize(FinalPacket.GetData());
    FMemory::Memcpy(FinalPacket.GetData() + FPacketHeader::Size, Result.GetData(), Result.Num());

    uint32 Sign = FCRC32C::Compute(FinalPacket.GetData(), FinalPacket.Num());
    FinalPacket.Append(reinterpret_cast<uint8*>(&Sign), sizeof(uint32));

    if (Header.Sequence == 0)
    {
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Final Packet Size: %d bytes (Header: %d + Ciphertext: %d + CRC32: 4)"),
            FinalPacket.Num(), FPacketHeader::Size, Result.Num()));
        ClientFileLog(FString::Printf(TEXT("[CLIENT] CRC32 Signature: %08X"), Sign));
        ClientFileLogHex(TEXT("[CLIENT] Complete Packet"), FinalPacket);
    }

    int32 BytesSent = 0;
    Socket->SendTo(FinalPacket.GetData(), FinalPacket.Num(), BytesSent, *RemoteEndpoint);

    if (reliable)
    {
        FReliablePacketInfo PacketInfo;
        PacketInfo.Buffer = FinalPacket;
        PacketInfo.SentTime = FPlatformTime::Seconds();
        PacketInfo.RetryCount = 0;
        PacketInfo.Sequence = Header.Sequence;
        ReliablePackets.Add(Header.Sequence, PacketInfo);

        UE_LOG(LogTemp, Log, TEXT("SendEncrypted: Sent reliable packet %d bytes, sequence %llu"), BytesSent, (unsigned long long)Header.Sequence);
    }
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

                bool bIsEncryptedPacket = false;
                FPacketHeader Header;

                if (BytesRead >= FPacketHeader::Size)
                {
                    Header = FPacketHeader::Deserialize(Buffer->GetRawBuffer());

                    if ((Header.Flags & EPacketHeaderFlags::Encrypted) != EPacketHeaderFlags::None &&
                        (Header.Flags & EPacketHeaderFlags::AEAD_ChaCha20Poly1305) != EPacketHeaderFlags::None &&
                        Header.ConnectionId == SecureSession.GetConnectionId())
                    {
                        bIsEncryptedPacket = true;
                    }
                }

                if (bIsEncryptedPacket)
                {
                    ProcessEncryptedPacket(Buffer, BytesRead, Header);
                }
                else
                {

                    EPacketType PacketType = static_cast<EPacketType>(Buffer->ReadByte());

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
                            if (!IsCryptoReady())
                            {
                                UE_LOG(LogTemp, Warning, TEXT("Dropping Unreliable packet before crypto handshake complete"));
                                break;
                            }
                            if (OnDataReceive)
                                OnDataReceive(Buffer);
                        }
                        break;
                        case EPacketType::Reliable:
                        {
                            if (!IsCryptoReady())
                            {
                                UE_LOG(LogTemp, Warning, TEXT("Dropping Reliable packet before crypto handshake complete"));
                                break;
                            }
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

                                if (SecureSession.InitializeAsClient(ClientPrivateKey, ServerPublicKey, Salt, connectionID))
                                {
                                    bEncryptionEnabled = true;
                                    UE_LOG(LogTemp, Log, TEXT("Secure session initialized successfully for connection %u"), connectionID);

                                    // Reset crypto handshake state
                                    bClientCryptoConfirmed = false;
                                    bServerCryptoConfirmed = false;
                                    bHandshakeComplete = false;

                                    ClientTestValue = 0xA1B2C3D4;
                                    UFlatBuffer* TestBuffer = UFlatBuffer::CreateFlatBuffer(8);
                                    // Fixed: Use simplified structure for CryptoTest
                                    TestBuffer->WriteByte(static_cast<uint8>(EPacketType::CryptoTest));
                                    TestBuffer->WriteUInt32(ClientTestValue);

                                    ClientFileLog(FString::Printf(TEXT("=== CRYPTO HANDSHAKE START ===")));
                                    ClientFileLog(FString::Printf(TEXT("[CLIENT] 🔐 Sending CryptoTest: %u"), ClientTestValue));
                                    ClientFileLog(FString::Printf(TEXT("[CLIENT] 🔐 Buffer size: %d bytes"), TestBuffer->GetLength()));

                                    SendLegacy(TestBuffer); // Use legacy for handshake compatibility
                                    UE_LOG(LogTemp, Log, TEXT("Client sent CryptoTest %u"), ClientTestValue);
                                }
                                else
                                {
                                    UE_LOG(LogTemp, Error, TEXT("Failed to initialize secure session"));
                                    Disconnect();
                                }
                            }
                        }
                        break;
                        case EPacketType::CryptoTestAck:
                        {
                            uint32 value = Buffer->ReadUInt32();
                            UE_LOG(LogTemp, Log, TEXT("Received CryptoTestAck %u (expected %u)"), value, ClientTestValue);
                            if (value == ClientTestValue)
                            {
                                bClientCryptoConfirmed = true;
                                UE_LOG(LogTemp, Log, TEXT("Client crypto confirmed! ServerConfirmed=%s"), bServerCryptoConfirmed ? TEXT("true") : TEXT("false"));
                            }
                            else
                            {
                                UE_LOG(LogTemp, Error, TEXT("CryptoTestAck value mismatch! Expected %u, got %u"), ClientTestValue, value);
                            }

                        }
                        break;
                        case EPacketType::CryptoTest:
                        {
                            uint32 value = Buffer->ReadUInt32();
                            UE_LOG(LogTemp, Log, TEXT("Received CryptoTest %u from server"), value);
                            ServerTestValue = value;
                            UFlatBuffer* AckBuffer = UFlatBuffer::CreateFlatBuffer(8);
                            AckBuffer->WriteByte(static_cast<uint8>(EPacketType::CryptoTestAck));
                            AckBuffer->WriteUInt32(value);

                            ClientFileLog(FString::Printf(TEXT("[CLIENT] 🔐 Sending CryptoTestAck: %u"), value));
                            ClientFileLog(FString::Printf(TEXT("[CLIENT] 🔐 Server crypto confirmed: %s"), bServerCryptoConfirmed ? TEXT("YES") : TEXT("NO")));

                            Send(AckBuffer);
                            UE_LOG(LogTemp, Log, TEXT("Sent CryptoTestAck %u to server"), value);
                            bServerCryptoConfirmed = true;
                            UE_LOG(LogTemp, Log, TEXT("Server crypto confirmed! ClientConfirmed=%s"), bClientCryptoConfirmed ? TEXT("true") : TEXT("false"));
                            if (IsCryptoReady() && !bHandshakeComplete)
                            {
                                bHandshakeComplete = true;
                                UE_LOG(LogTemp, Log, TEXT("Client crypto handshake complete"));

                                // Fixed: Use simplified structure for reliable handshake
                                UFlatBuffer* HandshakeBuffer = UFlatBuffer::CreateFlatBuffer(2);
                                HandshakeBuffer->WriteByte(static_cast<uint8>(EPacketType::ReliableHandshake));
                                HandshakeBuffer->WriteByte(0x01); // Handshake marker

                                ClientFileLog(FString::Printf(TEXT("=== RELIABLE HANDSHAKE START ===")));
                                ClientFileLog(FString::Printf(TEXT("[CLIENT] 🤝 Crypto ready: %s"), IsCryptoReady() ? TEXT("YES") : TEXT("NO")));
                                ClientFileLog(FString::Printf(TEXT("[CLIENT] 🤝 Sending ReliableHandshake packet (2 bytes)")));
                                ClientFileLog(FString::Printf(TEXT("[CLIENT] 🤝 Buffer: [0x%02X, 0x01]"), static_cast<uint8>(EPacketType::ReliableHandshake)));

                                UE_LOG(LogTemp, Log, TEXT("Attempting to send reliable handshake packet, IsCryptoReady: %s"), IsCryptoReady() ? TEXT("true") : TEXT("false"));
                                SendLegacy(HandshakeBuffer); // Use legacy for compatibility
                                UE_LOG(LogTemp, Log, TEXT("Sent reliable handshake packet"));

                                if (OnConnect)
                                    OnConnect(SecureSession.GetConnectionId());
                            }
                        }
                        break;
                        case EPacketType::ReliableHandshake:
                        {
                            ClientFileLog(FString::Printf(TEXT("=== RELIABLE HANDSHAKE RESPONSE ===")));
                            ClientFileLog(FString::Printf(TEXT("[CLIENT] 🤝 Received ReliableHandshake response from server")));

                            UE_LOG(LogTemp, Log, TEXT("Received reliable handshake response from server"));
                            bReliableHandshakeComplete = true;

                            if (IsFullyConnected())
                            {
                                ClientFileLog(FString::Printf(TEXT("[CLIENT] 🤝 Full connection established (crypto + reliable)")));
                                ClientFileLog(FString::Printf(TEXT("[CLIENT] 🤝 Connection ID: %u"), SecureSession.GetConnectionId()));

                                UE_LOG(LogTemp, Log, TEXT("Full connection established (crypto + reliable)"));
                                if (OnConnect)
                                    OnConnect(SecureSession.GetConnectionId());
                            }
                        }
                        break;
                        case EPacketType::Cookie:
                        {
                            if (BytesRead == 1 + 48 && !bCookieReceived)
                            {
                                const int32 Remaining = Buffer->Remaining();

                                if (Remaining < 48)
                                {
                                    UE_LOG(LogTemp, Warning, TEXT("Cookie truncated: remaining=%d"), Remaining);
                                    return;
                                }

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
                        case EPacketType::Ack:
                        {
                            // Read ulong as two uint32 parts (as sent by server)
                            uint32 low = Buffer->ReadUInt32();
                            uint32 high = Buffer->ReadUInt32();
                            uint64 sequence = ((uint64)high << 32) | low;
                            AcknowledgeReliablePacket(sequence);
                            UE_LOG(LogTemp, Verbose, TEXT("Received ACK for sequence %llu"), (unsigned long long)sequence);
                        }
                        break;
                    }
                }
            }
        }
    }
}

void UDPClient::ProcessEncryptedPacket(UFlatBuffer* Buffer, int32 BytesRead, const FPacketHeader& Header)
{
    if (Header.ConnectionId != SecureSession.GetConnectionId())
    {
        UE_LOG(LogTemp, Warning, TEXT("Received packet for wrong connection ID"));
        return;
    }

    if (!IsCryptoReady())
    {
        UE_LOG(LogTemp, Warning, TEXT("Dropping encrypted packet before crypto handshake complete"));
        return;
    }

    int32 PayloadSize = BytesRead - FPacketHeader::Size - 4;
    if (PayloadSize <= 16)
    {
        UE_LOG(LogTemp, Warning, TEXT("Encrypted packet payload too small"));
        return;
    }

    TArray<uint8> Payload;
    Payload.SetNumUninitialized(PayloadSize);
    FMemory::Memcpy(Payload.GetData(), Buffer->GetRawBuffer() + FPacketHeader::Size, PayloadSize);

    TArray<uint8> AAD = Header.GetAAD();
    bool bIsCompressed = (Header.Flags & EPacketHeaderFlags::Compressed) != EPacketHeaderFlags::None;
    bool bIsAcknowledgment = (Header.Flags & EPacketHeaderFlags::Acknowledgment) != EPacketHeaderFlags::None;
    TArray<uint8> Plaintext;

    if (!SecureSession.DecryptPayloadWithDecompression(Payload, AAD, Header.Sequence, bIsCompressed, Plaintext))
    {
        UE_LOG(LogTemp, Error, TEXT("Failed to decrypt packet"));
        return;
    }

    if (bIsAcknowledgment)
    {
        AcknowledgeReliablePacket(Header.Sequence);
        return;
    }

    if (Header.Channel == EPacketChannel::ReliableOrdered)
    {
        ProcessReliablePacket(Header.Sequence, Plaintext);
    }
    else
    {
        UnreliableEventQueue.Enqueue(Plaintext);
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

    FString PubKeyHex, PrivKeyHex;
    for (int32 i = 0; i < 32; i++)
    {
        PubKeyHex += FString::Printf(TEXT("%02X"), ClientPublicKey[i]);
        PrivKeyHex += FString::Printf(TEXT("%02X"), ClientPrivateKey[i]);
    }

    UE_LOG(LogTemp, Log, TEXT("[CRYPTO] Generated Client public key: %s"), *PubKeyHex);
    UE_LOG(LogTemp, Log, TEXT("[CRYPTO] Generated Client private key: %s"), *PrivKeyHex);

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
        {
            Client->PollIncomingPackets();
            Client->UpdateReliablePackets();
            Client->ProcessReliableQueue();
            Client->ProcessUnreliableQueue();
        }

        FPlatformProcess::Sleep(0.001f);
    }

    return 0;
}

void UDPClient::SendReliablePacket(const TArray<uint8>& Data)
{
    if (!Socket || !RemoteEndpoint.IsValid() || !IsCryptoReady())
        return;

    UFlatBuffer* Buffer = UFlatBuffer::CreateFlatBuffer(Data.Num() + 1);
    Buffer->WriteByte(static_cast<uint8>(EPacketType::ReliableHandshake));
    Buffer->WriteBytes(Data.GetData(), Data.Num());

    SendEncrypted(Buffer, true);
}

void UDPClient::SendUnreliablePacket(const TArray<uint8>& Data)
{
    if (!Socket || !RemoteEndpoint.IsValid() || !IsCryptoReady())
        return;

    UFlatBuffer* Buffer = UFlatBuffer::CreateFlatBuffer(Data.Num() + 1);
    Buffer->WriteByte(static_cast<uint8>(EPacketType::ReliableHandshake));
    Buffer->WriteBytes(Data.GetData(), Data.Num());

    SendEncrypted(Buffer, false);
}

void UDPClient::ProcessReliablePacket(uint64 Sequence, const TArray<uint8>& Data)
{
    SendAcknowledgment(Sequence);

    if (Sequence == ReliableSequenceReceive + 1)
    {
        ReliableSequenceReceive = Sequence;
        ReliableEventQueue.Enqueue(Data);

        while (ReliablePacketBuffer.Contains(ReliableSequenceReceive + 1))
        {
            TArray<uint8> NextData = ReliablePacketBuffer[ReliableSequenceReceive + 1];
            ReliablePacketBuffer.Remove(ReliableSequenceReceive + 1);
            ReliableSequenceReceive++;
            ReliableEventQueue.Enqueue(NextData);
        }
    }
    else if (Sequence > ReliableSequenceReceive + 1)
    {
        ReliablePacketBuffer.Add(Sequence, Data);
    }
}

void UDPClient::AcknowledgeReliablePacket(uint64 Sequence)
{
    if (ReliablePackets.Contains(Sequence))
    {
        ReliablePackets.Remove(Sequence);
        UE_LOG(LogTemp, Verbose, TEXT("Acknowledged reliable packet %llu"), (unsigned long long)Sequence);
    }
}

void UDPClient::SendAcknowledgment(uint64 Sequence)
{
    UFlatBuffer* AckBuffer = UFlatBuffer::CreateFlatBuffer(10);
    AckBuffer->WriteByte(static_cast<uint8>(EPacketType::Ack));
    AckBuffer->WriteUInt32(static_cast<uint32>(Sequence & 0xFFFFFFFF));
    AckBuffer->WriteUInt32(static_cast<uint32>((Sequence >> 32) & 0xFFFFFFFF));
    SendLegacy(AckBuffer);
    UE_LOG(LogTemp, Verbose, TEXT("Client sent ACK for sequence %llu"), (unsigned long long)Sequence);
}

void UDPClient::ProcessReliableQueue()
{
    TArray<uint8> Data;
    while (ReliableEventQueue.Dequeue(Data))
    {
        if (OnDataReceive)
        {
            UFlatBuffer* Buffer = UFlatBuffer::CreateFlatBuffer(Data.Num());
            Buffer->CopyFromMemory(Data.GetData(), Data.Num());
            ProcessMultiplePacketsInBuffer(Buffer);
        }
    }
}

void UDPClient::ProcessUnreliableQueue()
{
    TArray<uint8> Data;
    while (UnreliableEventQueue.Dequeue(Data))
    {
        if (OnDataReceive)
        {
            UFlatBuffer* Buffer = UFlatBuffer::CreateFlatBuffer(Data.Num());
            Buffer->CopyFromMemory(Data.GetData(), Data.Num());
            ProcessMultiplePacketsInBuffer(Buffer);
        }
    }
}

void UDPClient::ProcessMultiplePacketsInBuffer(UFlatBuffer* Buffer)
{
    if (!Buffer || Buffer->GetLength() <= 0)
        return;

    int32 OriginalPosition = Buffer->GetPosition();

    Buffer->SetPosition(0);

    int32 ProcessedPackets = 0;
    int32 TotalLength = Buffer->GetLength();

    ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] 📦 Iniciando processamento em lote. Tamanho do buffer: %d bytes"), TotalLength));

    if (Buffer->Remaining() < 1)
    {
        Buffer->SetPosition(OriginalPosition);
        OnDataReceive(Buffer);
        return;
    }

    EPacketType MainPacketType = static_cast<EPacketType>(Buffer->ReadByte());

    if (MainPacketType != EPacketType::Reliable && MainPacketType != EPacketType::Unreliable)
    {
        Buffer->SetPosition(OriginalPosition);
        OnDataReceive(Buffer);
        ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] ⚠️ Pacote principal não é Reliable/Unreliable: %d, processando normalmente"),
            static_cast<int32>(MainPacketType)));
        return;
    }

    UFlatBuffer* OriginalBuffer = UFlatBuffer::CreateFlatBuffer(TotalLength);
    OriginalBuffer->CopyFromMemory(Buffer->GetRawBuffer(), TotalLength);
    OriginalBuffer->SetPosition(1);

    ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] 🔄 Processando buffer com %d bytes"), TotalLength));

    while (OriginalBuffer->Remaining() >= 2)
    {
        int32 SubPacketStartPosition = OriginalBuffer->GetPosition();
        uint16 ServerPacketType = OriginalBuffer->ReadUInt16();

        ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] 📦 Subpacote #%d: Tipo=%d, Posição=%d"),
            ProcessedPackets + 1, ServerPacketType, SubPacketStartPosition));

        UFlatBuffer* SinglePacketBuffer = UFlatBuffer::CreateFlatBuffer(TotalLength);
        SinglePacketBuffer->WriteByte(static_cast<uint8>(MainPacketType));
        SinglePacketBuffer->WriteUInt16(ServerPacketType);

        int32 CurrentPosition = OriginalBuffer->GetPosition();
        int32 RemainingBytes = OriginalBuffer->Remaining();

        int32 PacketDataSize = 0;

        switch (static_cast<EServerPackets>(ServerPacketType))
        {
            case EServerPackets::UpdateEntityQuantized:
            {
                FUpdateEntityQuantizedPacket packet;
                packet.Deserialize(OriginalBuffer);
                PacketDataSize = packet.GetSize();

                ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] 📊 UpdateEntityQuantized: EntityId=%d, Size=%d"),
                    packet.EntityId, PacketDataSize));

                OriginalBuffer->SetPosition(CurrentPosition);
                break;
            }
            case EServerPackets::CreateEntity:
            {
                FCreateEntityPacket packet;
                packet.Deserialize(OriginalBuffer);
                PacketDataSize = packet.GetSize();

                ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] 📊 CreateEntity: EntityId=%d, Size=%d"),
                    packet.EntityId, PacketDataSize));

                OriginalBuffer->SetPosition(CurrentPosition);
                break;
            }
            case EServerPackets::RemoveEntity:
            {
                FRemoveEntityPacket packet;
                packet.Deserialize(OriginalBuffer);
                PacketDataSize = packet.GetSize();

                ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] 📊 RemoveEntity: EntityId=%d, Size=%d"),
                    packet.EntityId, PacketDataSize));

                OriginalBuffer->SetPosition(CurrentPosition);
                break;
            }
            default:
            {
                PacketDataSize = FMath::Min(24, RemainingBytes);
                ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] ⚠️ Tipo de pacote desconhecido: %d, usando tamanho padrão: %d"),
                    ServerPacketType, PacketDataSize));
                break;
            }
        }

        if (PacketDataSize > 0 && PacketDataSize <= RemainingBytes)
        {
            uint8* DataPtr = OriginalBuffer->GetRawBuffer() + CurrentPosition;
            SinglePacketBuffer->WriteBytes(DataPtr, PacketDataSize);
            OriginalBuffer->SetPosition(CurrentPosition + PacketDataSize);
        }
        else
        {
            ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] ⚠️ Tamanho de pacote inválido: %d (restante: %d)"),
                PacketDataSize, RemainingBytes));

            uint8* DataPtr = OriginalBuffer->GetRawBuffer() + CurrentPosition;
            SinglePacketBuffer->WriteBytes(DataPtr, RemainingBytes);
            OriginalBuffer->SetPosition(CurrentPosition + RemainingBytes);
        }

        SinglePacketBuffer->SetPosition(0);

        if (ProcessedPackets < 3)
        {
            FString BufferDump = TEXT("Buffer: [");
            for (int32 j = 0; j < FMath::Min(SinglePacketBuffer->GetLength(), 20); j++)
            {
                BufferDump += FString::Printf(TEXT("%02X "), SinglePacketBuffer->GetRawBuffer()[j]);
            }
            if (SinglePacketBuffer->GetLength() > 20) BufferDump += TEXT("...");
            BufferDump += TEXT("]");
            ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] 🔍 %s"), *BufferDump));
        }

        OnDataReceive(SinglePacketBuffer);
        ProcessedPackets++;

        if (OriginalBuffer->Remaining() <= 0)
            break;
    }

    ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] ✅ Total de subpacotes processados: %d"), ProcessedPackets));
}

void UDPClient::UpdateReliablePackets()
{
    double CurrentTime = FPlatformTime::Seconds();
    const double RetransmitTimeout = 0.25; // 250ms
    const int32 MaxRetries = 10;

    TArray<uint64> PacketsToRetry;

    for (const auto& Pair : ReliablePackets)
    {
        const FReliablePacketInfo& Info = Pair.Value;

        if (CurrentTime - Info.SentTime >= RetransmitTimeout)
        {
            PacketsToRetry.Add(Pair.Key);
        }
    }

    for (uint64 Sequence : PacketsToRetry)
    {
        if (ReliablePackets.Contains(Sequence))
        {
            FReliablePacketInfo& Info = ReliablePackets[Sequence];
            Info.RetryCount++;

            if (Info.RetryCount >= MaxRetries)
            {
                UE_LOG(LogTemp, Warning, TEXT("Reliable packet %llu dropped after %d retries"), (unsigned long long)Sequence, MaxRetries);
                ReliablePackets.Remove(Sequence);
                continue;
            }

            // Resend the packet
            int32 BytesSent = 0;
            Socket->SendTo(Info.Buffer.GetData(), Info.Buffer.Num(), BytesSent, *RemoteEndpoint);
            Info.SentTime = CurrentTime;

            if (Info.RetryCount > 3)
            {
                UE_LOG(LogTemp, Log, TEXT("Reliable packet %llu retry #%d"), (unsigned long long)Sequence, Info.RetryCount);
            }
        }
    }
}

