/*
 * ENetSubsystem.h
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
#include "Network/UDPClient.h"
#include "Network/UFlatBuffer.h"
#include "UObject/Object.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "Containers/Ticker.h"
#include "Sockets.h"
#include "SocketSubsystem.h"
#include "IPAddress.h"
#include "Modules/ModuleManager.h"
//%INCLUDES%
#include "ENetSubsystem.generated.h"

UCLASS(DisplayName = "ENetSubSystem")
class TOS_NETWORK_API UENetSubsystem : public UGameInstanceSubsystem
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;

	DECLARE_DYNAMIC_MULTICAST_DELEGATE(FOnDisconnected);
	DECLARE_DYNAMIC_MULTICAST_DELEGATE(FOnUDPConnectionError);
    DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnDataReceived, UFlatBuffer*, Buffer);
	DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnConnect, const int32&, ClientID);
	DECLARE_DYNAMIC_MULTICAST_DELEGATE(FOnConnectDenied);
//%DELEGATES%

	UPROPERTY(BlueprintAssignable, Category = "UDP")
	FOnUDPConnectionError OnConnectionError;

	UPROPERTY(BlueprintAssignable, Category = "UDP")
	FOnDisconnected OnDisconnected;

	UPROPERTY(BlueprintAssignable, Category = "UDP")
	FOnDataReceived OnDataReceived;

	UPROPERTY(BlueprintAssignable, Category = "UDP")
	FOnConnect OnConnect;

	UPROPERTY(BlueprintAssignable, Category = "UDP")
	FOnConnectDenied OnConnectDenied;

	UFUNCTION(BlueprintCallable, Category = "UDP")
	bool Connect(const FString& Host, int32 Port);

	UFUNCTION(BlueprintCallable, Category = "UDP")
	void Disconnect();

	UFUNCTION(BlueprintCallable, Category = "UDP")
	bool IsConnected() const { return bIsConnected; }

	UFUNCTION(BlueprintCallable, Category = "UDP")
	bool IsConnecting() const { return bIsConnecting; }

	UFUNCTION(BlueprintCallable, Category = "UDP")
	EConnectionStatus GetConnectionStatus() const { return ConnectionStatus; }

	UFUNCTION(BlueprintCallable, Category = "UDP")
	void SetConnectTimeout(float Seconds);

	UFUNCTION(BlueprintCallable, Category = "UDP")
	void SetRetryInterval(float Seconds);

	UFUNCTION(BlueprintCallable, Category = "UDP")
	void SetRetryEnabled(bool bEnabled);

	UFUNCTION(BlueprintCallable, Category = "UDP")
	float GetConnectTimeout() const;

	UFUNCTION(BlueprintCallable, Category = "UDP")
	float GetRetryInterval() const;

	UFUNCTION(BlueprintCallable, Category = "UDP")
	bool IsRetryEnabled() const;

    void SendEntitySync(FVector Position, FRotator Rotation, int32 AnimID, FVector Velocity) const;

//%EVENTS%

private:
	FTSTicker::FDelegateHandle TickHandle;
	bool Tick(float DeltaTime);

	TUniquePtr<class UDPClient> UdpClient;

	FSocket* Socket = nullptr;
	TSharedPtr<FInternetAddr> RemoteEndpoint;
	FThreadSafeBool bIsConnected = false;
	FThreadSafeBool bIsConnecting = false;
	EConnectionStatus ConnectionStatus = EConnectionStatus::Disconnected;
};
