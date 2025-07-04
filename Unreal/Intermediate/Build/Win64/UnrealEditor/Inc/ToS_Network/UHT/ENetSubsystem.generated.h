// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

// IWYU pragma: private, include "ENetSubsystem.h"
#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
class UByteBuffer;
enum class EConnectionStatus : uint8;
#ifdef TOS_NETWORK_ENetSubsystem_generated_h
#error "ENetSubsystem.generated.h already included, missing '#pragma once' in ENetSubsystem.h"
#endif
#define TOS_NETWORK_ENetSubsystem_generated_h

#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_46_DELEGATE \
static void FOnDisconnected_DelegateWrapper(const FMulticastScriptDelegate& OnDisconnected);


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_47_DELEGATE \
static void FOnUDPConnectionError_DelegateWrapper(const FMulticastScriptDelegate& OnUDPConnectionError);


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_48_DELEGATE \
static void FOnDataReceived_DelegateWrapper(const FMulticastScriptDelegate& OnDataReceived, UByteBuffer* Buffer);


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_49_DELEGATE \
static void FOnConnect_DelegateWrapper(const FMulticastScriptDelegate& OnConnect, int32 const& ClientID);


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_50_DELEGATE \
static void FOnConnectDenied_DelegateWrapper(const FMulticastScriptDelegate& OnConnectDenied);


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_40_RPC_WRAPPERS_NO_PURE_DECLS \
	DECLARE_FUNCTION(execIsRetryEnabled); \
	DECLARE_FUNCTION(execGetRetryInterval); \
	DECLARE_FUNCTION(execGetConnectTimeout); \
	DECLARE_FUNCTION(execSetRetryEnabled); \
	DECLARE_FUNCTION(execSetRetryInterval); \
	DECLARE_FUNCTION(execSetConnectTimeout); \
	DECLARE_FUNCTION(execGetConnectionStatus); \
	DECLARE_FUNCTION(execIsConnecting); \
	DECLARE_FUNCTION(execIsConnected); \
	DECLARE_FUNCTION(execDisconnect); \
	DECLARE_FUNCTION(execConnect);


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_40_INCLASS_NO_PURE_DECLS \
private: \
	static void StaticRegisterNativesUENetSubsystem(); \
	friend struct Z_Construct_UClass_UENetSubsystem_Statics; \
public: \
	DECLARE_CLASS(UENetSubsystem, UGameInstanceSubsystem, COMPILED_IN_FLAGS(0), CASTCLASS_None, TEXT("/Script/ToS_Network"), NO_API) \
	DECLARE_SERIALIZER(UENetSubsystem)


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_40_ENHANCED_CONSTRUCTORS \
	/** Standard constructor, called after all reflected properties have been initialized */ \
	NO_API UENetSubsystem(); \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	UENetSubsystem(UENetSubsystem&&); \
	UENetSubsystem(const UENetSubsystem&); \
public: \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, UENetSubsystem); \
	DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(UENetSubsystem); \
	DEFINE_DEFAULT_CONSTRUCTOR_CALL(UENetSubsystem) \
	NO_API virtual ~UENetSubsystem();


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_37_PROLOG
#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_40_GENERATED_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_40_RPC_WRAPPERS_NO_PURE_DECLS \
	FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_40_INCLASS_NO_PURE_DECLS \
	FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_40_ENHANCED_CONSTRUCTORS \
private: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


template<> TOS_NETWORK_API UClass* StaticClass<class UENetSubsystem>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h


PRAGMA_ENABLE_DEPRECATION_WARNINGS
