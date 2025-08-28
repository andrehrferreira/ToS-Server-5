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

    // Send packets encrypted if crypto is ready, otherwise use legacy
    if (IsCryptoReady())
    {
        SendEncrypted(buffer, false); // Unreliable encrypted
    }
    else
    {
        SendLegacy(buffer); // Fallback to unencrypted during handshake
    }
}

void UDPClient::SendEncrypted(UFlatBuffer* buffer, bool reliable)
{
    TArray<uint8> Payload;
    Payload.SetNumUninitialized(buffer->GetLength());
    FMemory::Memcpy(Payload.GetData(), buffer->GetRawBuffer(), buffer->GetLength());

    // Create a temporary header to get the sequence BEFORE encryption
    FPacketHeader Header;
    Header.ConnectionId = SecureSession.GetConnectionId();
    Header.Channel = reliable ? EPacketChannel::ReliableOrdered : EPacketChannel::Unreliable;
    Header.Flags = EPacketHeaderFlags::Encrypted | EPacketHeaderFlags::AEAD_ChaCha20Poly1305;
    Header.Sequence = SecureSession.GetSeqTx(); // This will be the sequence used for encryption

    // Remove verbose debug logs since crypto is working

    TArray<uint8> AAD = Header.GetAAD();
    TArray<uint8> Result;
    bool bWasCompressed = false;

    // Log crypto data to file for first packet
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

               // Log position update packets (unreliable channel with specific patterns)
           if (Header.Channel == EPacketChannel::Unreliable && Header.Sequence > 0 && Header.Sequence <= 10)
           {
               // Identify packet type from payload
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

               // Log COMPLETE byte-by-byte analysis
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

               // Interpret key fields
               if (Payload.Num() >= 3)
               {
                   uint16 ClientPacketValue = (uint16)Payload[1] | ((uint16)Payload[2] << 8);
                   ClientFileLog(FString::Printf(TEXT("[CLIENT] ClientPacket interpreted as uint16: %d"), ClientPacketValue));
                   ClientFileLog(FString::Printf(TEXT("[CLIENT] Should be 0 for SyncEntity: %s"), (ClientPacketValue == 0) ? TEXT("YES") : TEXT("NO")));
               }
           }

    // Now encrypt - this will increment SeqTx internally
    if (!SecureSession.EncryptPayloadWithCompression(Payload, AAD, Result, bWasCompressed))
    {
        UE_LOG(LogTemp, Error, TEXT("Failed to encrypt payload"));
        return;
    }

    if (bWasCompressed)
        Header.Flags |= EPacketHeaderFlags::Compressed;

        // Log encryption results for first packet
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

    // Log final packet info for first packet
    if (Header.Sequence == 0)
    {
        ClientFileLog(FString::Printf(TEXT("[CLIENT] Final Packet Size: %d bytes (Header: %d + Ciphertext: %d + CRC32: 4)"),
            FinalPacket.Num(), FPacketHeader::Size, Result.Num()));
        ClientFileLog(FString::Printf(TEXT("[CLIENT] CRC32 Signature: %08X"), Sign));
        ClientFileLogHex(TEXT("[CLIENT] Complete Packet"), FinalPacket);
    }

    int32 BytesSent = 0;
    Socket->SendTo(FinalPacket.GetData(), FinalPacket.Num(), BytesSent, *RemoteEndpoint);

    // Crypto working - removing verbose logs

    // Store for reliable tracking if needed
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

                // Check if this is an encrypted packet with new header format
                bool bIsEncryptedPacket = false;
                FPacketHeader Header;

                if (BytesRead >= FPacketHeader::Size)
                {
                    // Try to parse as new header format
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
                    // Process as legacy packet
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
                } // End of else block for legacy packets
            }
        }
    }
}

void UDPClient::ProcessEncryptedPacket(UFlatBuffer* Buffer, int32 BytesRead, const FPacketHeader& Header)
{
    // Verify this is our connection
    if (Header.ConnectionId != SecureSession.GetConnectionId())
    {
        UE_LOG(LogTemp, Warning, TEXT("Received packet for wrong connection ID"));
        return;
    }

    // Check if handshake is complete
    if (!IsCryptoReady())
    {
        UE_LOG(LogTemp, Warning, TEXT("Dropping encrypted packet before crypto handshake complete"));
        return;
    }

    // Extract payload (skip header)
    int32 PayloadSize = BytesRead - FPacketHeader::Size - 4; // -4 for CRC32
    if (PayloadSize <= 16) // ChaCha20Poly1305 adds 16 bytes
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

    // Handle acknowledgment packets
    if (bIsAcknowledgment)
    {
        AcknowledgeReliablePacket(Header.Sequence);
        return;
    }

    // Route to appropriate queue based on channel
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

    // Log client keys for debugging
    FString PubKeyHex, PrivKeyHex;
    for (int32 i = 0; i < 32; i++)
    {
        PubKeyHex += FString::Printf(TEXT("%02X"), ClientPublicKey[i]);
        PrivKeyHex += FString::Printf(TEXT("%02X"), ClientPrivateKey[i]);
    }
    UE_LOG(LogTemp, Log, TEXT("[CRYPTO] Generated Client public key: %s"), *PubKeyHex);
    UE_LOG(LogTemp, Log, TEXT("[CRYPTO] Generated Client private key: %s"), *PrivKeyHex);

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

    // Create UFlatBuffer with the data
    UFlatBuffer* Buffer = UFlatBuffer::CreateFlatBuffer(Data.Num() + 1);
    Buffer->WriteByte(static_cast<uint8>(EPacketType::ReliableHandshake));
    Buffer->WriteBytes(Data.GetData(), Data.Num());

    SendEncrypted(Buffer, true); // Use reliable=true
}

void UDPClient::SendUnreliablePacket(const TArray<uint8>& Data)
{
    if (!Socket || !RemoteEndpoint.IsValid() || !IsCryptoReady())
        return;

    // Create UFlatBuffer with the data
    UFlatBuffer* Buffer = UFlatBuffer::CreateFlatBuffer(Data.Num() + 1);
    Buffer->WriteByte(static_cast<uint8>(EPacketType::ReliableHandshake));
    Buffer->WriteBytes(Data.GetData(), Data.Num());

    SendEncrypted(Buffer, false); // Use reliable=false
}

void UDPClient::ProcessReliablePacket(uint64 Sequence, const TArray<uint8>& Data)
{
    // Send acknowledgment immediately when receiving reliable packet from server
    SendAcknowledgment(Sequence);

    // Check if this is the next expected sequence
    if (Sequence == ReliableSequenceReceive + 1)
    {
        // Process this packet and any buffered consecutive ones
        ReliableSequenceReceive = Sequence;
        ReliableEventQueue.Enqueue(Data);

        // Check for buffered packets
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
        // Buffer out-of-order packet
        ReliablePacketBuffer.Add(Sequence, Data);
    }
    // Else: Duplicate or old packet, discard
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
    // Client sends ACK when it receives reliable packets from server
    UFlatBuffer* AckBuffer = UFlatBuffer::CreateFlatBuffer(10);
    AckBuffer->WriteByte(static_cast<uint8>(EPacketType::Ack));
    AckBuffer->WriteUInt32(static_cast<uint32>(Sequence & 0xFFFFFFFF));       // Low 32 bits
    AckBuffer->WriteUInt32(static_cast<uint32>((Sequence >> 32) & 0xFFFFFFFF)); // High 32 bits
    SendLegacy(AckBuffer);
    UE_LOG(LogTemp, Verbose, TEXT("Client sent ACK for sequence %llu"), (unsigned long long)Sequence);
}

void UDPClient::ProcessReliableQueue()
{
    TArray<uint8> Data;
    while (ReliableEventQueue.Dequeue(Data))
    {
        // Process reliable game data here
        if (OnDataReceive)
        {
            UFlatBuffer* Buffer = UFlatBuffer::CreateFlatBuffer(Data.Num());
            Buffer->CopyFromMemory(Data.GetData(), Data.Num());

            // Processar múltiplos pacotes em um único buffer
            ProcessMultiplePacketsInBuffer(Buffer);
        }
    }
}

void UDPClient::ProcessUnreliableQueue()
{
    TArray<uint8> Data;
    while (UnreliableEventQueue.Dequeue(Data))
    {
        // Process unreliable game data here
        if (OnDataReceive)
        {
            UFlatBuffer* Buffer = UFlatBuffer::CreateFlatBuffer(Data.Num());
            Buffer->CopyFromMemory(Data.GetData(), Data.Num());

            // Processar múltiplos pacotes em um único buffer
            ProcessMultiplePacketsInBuffer(Buffer);
        }
    }
}

void UDPClient::ProcessMultiplePacketsInBuffer(UFlatBuffer* Buffer)
{
    if (!Buffer || Buffer->GetLength() <= 0)
        return;

    // Salvar a posição original do buffer
    int32 OriginalPosition = Buffer->GetPosition();

    // Resetar a posição para o início
    Buffer->SetPosition(0);

    // Processar enquanto houver dados suficientes no buffer
    int32 ProcessedPackets = 0;
    int32 TotalLength = Buffer->GetLength();

    ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] 📦 Iniciando processamento em lote. Tamanho do buffer: %d bytes"), TotalLength));

    // Primeiro, verificar se é um pacote Reliable ou Unreliable
    if (Buffer->Remaining() < 1)
    {
        Buffer->SetPosition(OriginalPosition);
        OnDataReceive(Buffer);
        return;
    }

    // Ler o tipo de pacote principal
    EPacketType MainPacketType = static_cast<EPacketType>(Buffer->ReadByte());

    // Verificar se é um pacote Reliable ou Unreliable
    if (MainPacketType != EPacketType::Reliable && MainPacketType != EPacketType::Unreliable)
    {
        // Se não for Reliable ou Unreliable, processar normalmente
        Buffer->SetPosition(OriginalPosition);
        OnDataReceive(Buffer);
        ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] ⚠️ Pacote principal não é Reliable/Unreliable: %d, processando normalmente"),
            static_cast<int32>(MainPacketType)));
        return;
    }

    // Agora vamos processar os subpacotes dentro do pacote principal
    while (Buffer->Remaining() >= 3) // Mínimo para um subpacote (2 bytes tipo + pelo menos 1 byte dados)
    {
        int32 SubPacketStartPosition = Buffer->GetPosition();

        // Ler o tipo de subpacote (EServerPackets)
        if (Buffer->Remaining() < 2)
        {
            // Não há bytes suficientes para ler o tipo de subpacote
            break;
        }

        uint16 ServerPacketTypeValue = Buffer->ReadUInt16();

        // Estimar o tamanho do subpacote com base no tipo
        // Aqui, vamos usar uma abordagem mais genérica sem switch de tipos de pacotes

        // Verificar se há pelo menos alguns bytes de dados disponíveis
        if (Buffer->Remaining() < 4) // Mínimo de 4 bytes para qualquer subpacote
        {
            // Não há bytes suficientes para um subpacote válido
            break;
        }

        // Determinar um tamanho estimado para o subpacote
        // Para evitar o switch, vamos usar um tamanho fixo e seguro para todos os subpacotes
        int32 EstimatedDataSize = 32; // Tamanho seguro para a maioria dos subpacotes

        // Verificar se há bytes suficientes disponíveis
        if (Buffer->Remaining() < EstimatedDataSize)
        {
            EstimatedDataSize = Buffer->Remaining(); // Usar o que estiver disponível
        }

        // Criar um novo buffer para este subpacote
        int32 EstimatedPacketSize = 3 + EstimatedDataSize; // 1 (tipo principal) + 2 (tipo subpacote) + dados

        UFlatBuffer* SinglePacketBuffer = UFlatBuffer::CreateFlatBuffer(EstimatedPacketSize);
        SinglePacketBuffer->WriteByte(static_cast<uint8>(MainPacketType)); // Manter o tipo principal
        SinglePacketBuffer->WriteUInt16(ServerPacketTypeValue); // Tipo de subpacote

        // Copiar os dados do subpacote
        SinglePacketBuffer->WriteBytes(Buffer->GetRawBuffer() + SubPacketStartPosition + 2, EstimatedDataSize);

        // Processar este subpacote individualmente
        SinglePacketBuffer->SetPosition(0);
        OnDataReceive(SinglePacketBuffer);

        // Avançar a posição no buffer principal
        Buffer->SetPosition(SubPacketStartPosition + 2 + EstimatedDataSize);
        ProcessedPackets++;

        ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] ✅ Subpacote #%d processado: Tipo=%d, Tamanho=%d bytes"),
            ProcessedPackets, ServerPacketTypeValue, EstimatedDataSize));
    }

    // Se ainda houver dados restantes, mas não suficientes para um subpacote completo
    if (Buffer->Remaining() > 0)
    {
        ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] ℹ️ Dados remanescentes insuficientes para um subpacote completo: %d bytes"),
            Buffer->Remaining()));
    }

    ClientFileLog(FString::Printf(TEXT("[BATCH PROCESSING] 📊 Total de subpacotes processados em lote: %d"), ProcessedPackets));
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

