// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

// IWYU pragma: private, include "NetTypes.h"
#include "Templates/IsUEnumClass.h"
#include "UObject/ObjectMacros.h"
#include "UObject/ReflectedTypeAccessors.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
#ifdef TOS_NETWORK_NetTypes_generated_h
#error "NetTypes.generated.h already included, missing '#pragma once' in NetTypes.h"
#endif
#define TOS_NETWORK_NetTypes_generated_h

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_NetTypes_h


#define FOREACH_ENUM_ECONNECTIONSTATUS(op) \
	op(EConnectionStatus::Disconnected) \
	op(EConnectionStatus::Connecting) \
	op(EConnectionStatus::Connected) \
	op(EConnectionStatus::ConnectionFailed) 

enum class EConnectionStatus : uint8;
template<> struct TIsUEnumClass<EConnectionStatus> { enum { Value = true }; };
template<> TOS_NETWORK_API UEnum* StaticEnum<EConnectionStatus>();

#define FOREACH_ENUM_EPACKETTYPE(op) \
	op(EPacketType::Connect) \
	op(EPacketType::Ping) \
	op(EPacketType::Pong) \
	op(EPacketType::Reliable) \
	op(EPacketType::Unreliable) \
	op(EPacketType::Ack) \
	op(EPacketType::Disconnect) \
	op(EPacketType::Error) \
	op(EPacketType::ConnectionDenied) \
	op(EPacketType::ConnectionAccepted) 

enum class EPacketType : uint8;
template<> struct TIsUEnumClass<EPacketType> { enum { Value = true }; };
template<> TOS_NETWORK_API UEnum* StaticEnum<EPacketType>();

PRAGMA_ENABLE_DEPRECATION_WARNINGS
