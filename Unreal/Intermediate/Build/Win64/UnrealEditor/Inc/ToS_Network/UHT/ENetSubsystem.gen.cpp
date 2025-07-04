// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/GeneratedCppIncludes.h"
#include "ToS_Network/Public/ENetSubsystem.h"
#include "Runtime/Engine/Classes/Engine/GameInstance.h"
PRAGMA_DISABLE_DEPRECATION_WARNINGS
void EmptyLinkFunctionForGeneratedCodeENetSubsystem() {}

// Begin Cross Module References
ENGINE_API UClass* Z_Construct_UClass_UGameInstanceSubsystem();
TOS_NETWORK_API UClass* Z_Construct_UClass_UByteBuffer_NoRegister();
TOS_NETWORK_API UClass* Z_Construct_UClass_UENetSubsystem();
TOS_NETWORK_API UClass* Z_Construct_UClass_UENetSubsystem_NoRegister();
TOS_NETWORK_API UEnum* Z_Construct_UEnum_ToS_Network_EConnectionStatus();
TOS_NETWORK_API UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature();
TOS_NETWORK_API UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnConnectDenied__DelegateSignature();
TOS_NETWORK_API UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature();
TOS_NETWORK_API UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnDisconnected__DelegateSignature();
TOS_NETWORK_API UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnUDPConnectionError__DelegateSignature();
UPackage* Z_Construct_UPackage__Script_ToS_Network();
// End Cross Module References

// Begin Delegate FOnDisconnected
struct Z_Construct_UDelegateFunction_UENetSubsystem_OnDisconnected__DelegateSignature_Statics
{
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FFunctionParams Z_Construct_UDelegateFunction_UENetSubsystem_OnDisconnected__DelegateSignature_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "OnDisconnected__DelegateSignature", nullptr, nullptr, nullptr, 0, 0, RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x00130000, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UDelegateFunction_UENetSubsystem_OnDisconnected__DelegateSignature_Statics::Function_MetaDataParams), Z_Construct_UDelegateFunction_UENetSubsystem_OnDisconnected__DelegateSignature_Statics::Function_MetaDataParams) };
UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnDisconnected__DelegateSignature()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UDelegateFunction_UENetSubsystem_OnDisconnected__DelegateSignature_Statics::FuncParams);
	}
	return ReturnFunction;
}
void UENetSubsystem::FOnDisconnected_DelegateWrapper(const FMulticastScriptDelegate& OnDisconnected)
{
	OnDisconnected.ProcessMulticastDelegate<UObject>(NULL);
}
// End Delegate FOnDisconnected

// Begin Delegate FOnUDPConnectionError
struct Z_Construct_UDelegateFunction_UENetSubsystem_OnUDPConnectionError__DelegateSignature_Statics
{
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FFunctionParams Z_Construct_UDelegateFunction_UENetSubsystem_OnUDPConnectionError__DelegateSignature_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "OnUDPConnectionError__DelegateSignature", nullptr, nullptr, nullptr, 0, 0, RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x00130000, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UDelegateFunction_UENetSubsystem_OnUDPConnectionError__DelegateSignature_Statics::Function_MetaDataParams), Z_Construct_UDelegateFunction_UENetSubsystem_OnUDPConnectionError__DelegateSignature_Statics::Function_MetaDataParams) };
UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnUDPConnectionError__DelegateSignature()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UDelegateFunction_UENetSubsystem_OnUDPConnectionError__DelegateSignature_Statics::FuncParams);
	}
	return ReturnFunction;
}
void UENetSubsystem::FOnUDPConnectionError_DelegateWrapper(const FMulticastScriptDelegate& OnUDPConnectionError)
{
	OnUDPConnectionError.ProcessMulticastDelegate<UObject>(NULL);
}
// End Delegate FOnUDPConnectionError

// Begin Delegate FOnDataReceived
struct Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics
{
	struct ENetSubsystem_eventOnDataReceived_Parms
	{
		UByteBuffer* Buffer;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FObjectPropertyParams NewProp_Buffer;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FObjectPropertyParams Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics::NewProp_Buffer = { "Buffer", nullptr, (EPropertyFlags)0x0010000000000080, UECodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ENetSubsystem_eventOnDataReceived_Parms, Buffer), Z_Construct_UClass_UByteBuffer_NoRegister, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics::NewProp_Buffer,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "OnDataReceived__DelegateSignature", nullptr, nullptr, Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics::PropPointers), sizeof(Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics::ENetSubsystem_eventOnDataReceived_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x00130000, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics::Function_MetaDataParams), Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics::ENetSubsystem_eventOnDataReceived_Parms) < MAX_uint16);
UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature_Statics::FuncParams);
	}
	return ReturnFunction;
}
void UENetSubsystem::FOnDataReceived_DelegateWrapper(const FMulticastScriptDelegate& OnDataReceived, UByteBuffer* Buffer)
{
	struct ENetSubsystem_eventOnDataReceived_Parms
	{
		UByteBuffer* Buffer;
	};
	ENetSubsystem_eventOnDataReceived_Parms Parms;
	Parms.Buffer=Buffer;
	OnDataReceived.ProcessMulticastDelegate<UObject>(&Parms);
}
// End Delegate FOnDataReceived

// Begin Delegate FOnConnect
struct Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics
{
	struct ENetSubsystem_eventOnConnect_Parms
	{
		int32 ClientID;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
	static constexpr UECodeGen_Private::FMetaDataPairParam NewProp_ClientID_MetaData[] = {
		{ "NativeConst", "" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FIntPropertyParams NewProp_ClientID;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FIntPropertyParams Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics::NewProp_ClientID = { "ClientID", nullptr, (EPropertyFlags)0x0010000008000182, UECodeGen_Private::EPropertyGenFlags::Int, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ENetSubsystem_eventOnConnect_Parms, ClientID), METADATA_PARAMS(UE_ARRAY_COUNT(NewProp_ClientID_MetaData), NewProp_ClientID_MetaData) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics::NewProp_ClientID,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "OnConnect__DelegateSignature", nullptr, nullptr, Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics::PropPointers), sizeof(Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics::ENetSubsystem_eventOnConnect_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x00530000, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics::Function_MetaDataParams), Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics::ENetSubsystem_eventOnConnect_Parms) < MAX_uint16);
UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature_Statics::FuncParams);
	}
	return ReturnFunction;
}
void UENetSubsystem::FOnConnect_DelegateWrapper(const FMulticastScriptDelegate& OnConnect, int32 const& ClientID)
{
	struct ENetSubsystem_eventOnConnect_Parms
	{
		int32 ClientID;
	};
	ENetSubsystem_eventOnConnect_Parms Parms;
	Parms.ClientID=ClientID;
	OnConnect.ProcessMulticastDelegate<UObject>(&Parms);
}
// End Delegate FOnConnect

// Begin Delegate FOnConnectDenied
struct Z_Construct_UDelegateFunction_UENetSubsystem_OnConnectDenied__DelegateSignature_Statics
{
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FFunctionParams Z_Construct_UDelegateFunction_UENetSubsystem_OnConnectDenied__DelegateSignature_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "OnConnectDenied__DelegateSignature", nullptr, nullptr, nullptr, 0, 0, RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x00130000, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UDelegateFunction_UENetSubsystem_OnConnectDenied__DelegateSignature_Statics::Function_MetaDataParams), Z_Construct_UDelegateFunction_UENetSubsystem_OnConnectDenied__DelegateSignature_Statics::Function_MetaDataParams) };
UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnConnectDenied__DelegateSignature()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UDelegateFunction_UENetSubsystem_OnConnectDenied__DelegateSignature_Statics::FuncParams);
	}
	return ReturnFunction;
}
void UENetSubsystem::FOnConnectDenied_DelegateWrapper(const FMulticastScriptDelegate& OnConnectDenied)
{
	OnConnectDenied.ProcessMulticastDelegate<UObject>(NULL);
}
// End Delegate FOnConnectDenied

// Begin Class UENetSubsystem Function Connect
struct Z_Construct_UFunction_UENetSubsystem_Connect_Statics
{
	struct ENetSubsystem_eventConnect_Parms
	{
		FString Host;
		int32 Port;
		bool ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
	static constexpr UECodeGen_Private::FMetaDataPairParam NewProp_Host_MetaData[] = {
		{ "NativeConst", "" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FStrPropertyParams NewProp_Host;
	static const UECodeGen_Private::FIntPropertyParams NewProp_Port;
	static void NewProp_ReturnValue_SetBit(void* Obj);
	static const UECodeGen_Private::FBoolPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FStrPropertyParams Z_Construct_UFunction_UENetSubsystem_Connect_Statics::NewProp_Host = { "Host", nullptr, (EPropertyFlags)0x0010000000000080, UECodeGen_Private::EPropertyGenFlags::Str, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ENetSubsystem_eventConnect_Parms, Host), METADATA_PARAMS(UE_ARRAY_COUNT(NewProp_Host_MetaData), NewProp_Host_MetaData) };
const UECodeGen_Private::FIntPropertyParams Z_Construct_UFunction_UENetSubsystem_Connect_Statics::NewProp_Port = { "Port", nullptr, (EPropertyFlags)0x0010000000000080, UECodeGen_Private::EPropertyGenFlags::Int, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ENetSubsystem_eventConnect_Parms, Port), METADATA_PARAMS(0, nullptr) };
void Z_Construct_UFunction_UENetSubsystem_Connect_Statics::NewProp_ReturnValue_SetBit(void* Obj)
{
	((ENetSubsystem_eventConnect_Parms*)Obj)->ReturnValue = 1;
}
const UECodeGen_Private::FBoolPropertyParams Z_Construct_UFunction_UENetSubsystem_Connect_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Bool | UECodeGen_Private::EPropertyGenFlags::NativeBool, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, sizeof(bool), sizeof(ENetSubsystem_eventConnect_Parms), &Z_Construct_UFunction_UENetSubsystem_Connect_Statics::NewProp_ReturnValue_SetBit, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UENetSubsystem_Connect_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_Connect_Statics::NewProp_Host,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_Connect_Statics::NewProp_Port,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_Connect_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_Connect_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UENetSubsystem_Connect_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "Connect", nullptr, nullptr, Z_Construct_UFunction_UENetSubsystem_Connect_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_Connect_Statics::PropPointers), sizeof(Z_Construct_UFunction_UENetSubsystem_Connect_Statics::ENetSubsystem_eventConnect_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_Connect_Statics::Function_MetaDataParams), Z_Construct_UFunction_UENetSubsystem_Connect_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UENetSubsystem_Connect_Statics::ENetSubsystem_eventConnect_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UENetSubsystem_Connect()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UENetSubsystem_Connect_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UENetSubsystem::execConnect)
{
	P_GET_PROPERTY(FStrProperty,Z_Param_Host);
	P_GET_PROPERTY(FIntProperty,Z_Param_Port);
	P_FINISH;
	P_NATIVE_BEGIN;
	*(bool*)Z_Param__Result=P_THIS->Connect(Z_Param_Host,Z_Param_Port);
	P_NATIVE_END;
}
// End Class UENetSubsystem Function Connect

// Begin Class UENetSubsystem Function Disconnect
struct Z_Construct_UFunction_UENetSubsystem_Disconnect_Statics
{
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UENetSubsystem_Disconnect_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "Disconnect", nullptr, nullptr, nullptr, 0, 0, RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_Disconnect_Statics::Function_MetaDataParams), Z_Construct_UFunction_UENetSubsystem_Disconnect_Statics::Function_MetaDataParams) };
UFunction* Z_Construct_UFunction_UENetSubsystem_Disconnect()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UENetSubsystem_Disconnect_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UENetSubsystem::execDisconnect)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	P_THIS->Disconnect();
	P_NATIVE_END;
}
// End Class UENetSubsystem Function Disconnect

// Begin Class UENetSubsystem Function GetConnectionStatus
struct Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics
{
	struct ENetSubsystem_eventGetConnectionStatus_Parms
	{
		EConnectionStatus ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FBytePropertyParams NewProp_ReturnValue_Underlying;
	static const UECodeGen_Private::FEnumPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FBytePropertyParams Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::NewProp_ReturnValue_Underlying = { "UnderlyingType", nullptr, (EPropertyFlags)0x0000000000000000, UECodeGen_Private::EPropertyGenFlags::Byte, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, 0, nullptr, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FEnumPropertyParams Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Enum, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ENetSubsystem_eventGetConnectionStatus_Parms, ReturnValue), Z_Construct_UEnum_ToS_Network_EConnectionStatus, METADATA_PARAMS(0, nullptr) }; // 2044594972
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::NewProp_ReturnValue_Underlying,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "GetConnectionStatus", nullptr, nullptr, Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::PropPointers), sizeof(Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::ENetSubsystem_eventGetConnectionStatus_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x54020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::Function_MetaDataParams), Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::ENetSubsystem_eventGetConnectionStatus_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UENetSubsystem::execGetConnectionStatus)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(EConnectionStatus*)Z_Param__Result=P_THIS->GetConnectionStatus();
	P_NATIVE_END;
}
// End Class UENetSubsystem Function GetConnectionStatus

// Begin Class UENetSubsystem Function GetConnectTimeout
struct Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics
{
	struct ENetSubsystem_eventGetConnectTimeout_Parms
	{
		float ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FFloatPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FFloatPropertyParams Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Float, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ENetSubsystem_eventGetConnectTimeout_Parms, ReturnValue), METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "GetConnectTimeout", nullptr, nullptr, Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics::PropPointers), sizeof(Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics::ENetSubsystem_eventGetConnectTimeout_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x54020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics::Function_MetaDataParams), Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics::ENetSubsystem_eventGetConnectTimeout_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UENetSubsystem::execGetConnectTimeout)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(float*)Z_Param__Result=P_THIS->GetConnectTimeout();
	P_NATIVE_END;
}
// End Class UENetSubsystem Function GetConnectTimeout

// Begin Class UENetSubsystem Function GetRetryInterval
struct Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics
{
	struct ENetSubsystem_eventGetRetryInterval_Parms
	{
		float ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FFloatPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FFloatPropertyParams Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Float, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ENetSubsystem_eventGetRetryInterval_Parms, ReturnValue), METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "GetRetryInterval", nullptr, nullptr, Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics::PropPointers), sizeof(Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics::ENetSubsystem_eventGetRetryInterval_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x54020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics::Function_MetaDataParams), Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics::ENetSubsystem_eventGetRetryInterval_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UENetSubsystem_GetRetryInterval()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UENetSubsystem_GetRetryInterval_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UENetSubsystem::execGetRetryInterval)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(float*)Z_Param__Result=P_THIS->GetRetryInterval();
	P_NATIVE_END;
}
// End Class UENetSubsystem Function GetRetryInterval

// Begin Class UENetSubsystem Function IsConnected
struct Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics
{
	struct ENetSubsystem_eventIsConnected_Parms
	{
		bool ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static void NewProp_ReturnValue_SetBit(void* Obj);
	static const UECodeGen_Private::FBoolPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
void Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::NewProp_ReturnValue_SetBit(void* Obj)
{
	((ENetSubsystem_eventIsConnected_Parms*)Obj)->ReturnValue = 1;
}
const UECodeGen_Private::FBoolPropertyParams Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Bool | UECodeGen_Private::EPropertyGenFlags::NativeBool, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, sizeof(bool), sizeof(ENetSubsystem_eventIsConnected_Parms), &Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::NewProp_ReturnValue_SetBit, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "IsConnected", nullptr, nullptr, Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::PropPointers), sizeof(Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::ENetSubsystem_eventIsConnected_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x54020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::Function_MetaDataParams), Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::ENetSubsystem_eventIsConnected_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UENetSubsystem_IsConnected()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UENetSubsystem_IsConnected_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UENetSubsystem::execIsConnected)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(bool*)Z_Param__Result=P_THIS->IsConnected();
	P_NATIVE_END;
}
// End Class UENetSubsystem Function IsConnected

// Begin Class UENetSubsystem Function IsConnecting
struct Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics
{
	struct ENetSubsystem_eventIsConnecting_Parms
	{
		bool ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static void NewProp_ReturnValue_SetBit(void* Obj);
	static const UECodeGen_Private::FBoolPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
void Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::NewProp_ReturnValue_SetBit(void* Obj)
{
	((ENetSubsystem_eventIsConnecting_Parms*)Obj)->ReturnValue = 1;
}
const UECodeGen_Private::FBoolPropertyParams Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Bool | UECodeGen_Private::EPropertyGenFlags::NativeBool, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, sizeof(bool), sizeof(ENetSubsystem_eventIsConnecting_Parms), &Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::NewProp_ReturnValue_SetBit, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "IsConnecting", nullptr, nullptr, Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::PropPointers), sizeof(Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::ENetSubsystem_eventIsConnecting_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x54020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::Function_MetaDataParams), Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::ENetSubsystem_eventIsConnecting_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UENetSubsystem_IsConnecting()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UENetSubsystem_IsConnecting_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UENetSubsystem::execIsConnecting)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(bool*)Z_Param__Result=P_THIS->IsConnecting();
	P_NATIVE_END;
}
// End Class UENetSubsystem Function IsConnecting

// Begin Class UENetSubsystem Function IsRetryEnabled
struct Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics
{
	struct ENetSubsystem_eventIsRetryEnabled_Parms
	{
		bool ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static void NewProp_ReturnValue_SetBit(void* Obj);
	static const UECodeGen_Private::FBoolPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
void Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::NewProp_ReturnValue_SetBit(void* Obj)
{
	((ENetSubsystem_eventIsRetryEnabled_Parms*)Obj)->ReturnValue = 1;
}
const UECodeGen_Private::FBoolPropertyParams Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Bool | UECodeGen_Private::EPropertyGenFlags::NativeBool, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, sizeof(bool), sizeof(ENetSubsystem_eventIsRetryEnabled_Parms), &Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::NewProp_ReturnValue_SetBit, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "IsRetryEnabled", nullptr, nullptr, Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::PropPointers), sizeof(Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::ENetSubsystem_eventIsRetryEnabled_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x54020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::Function_MetaDataParams), Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::ENetSubsystem_eventIsRetryEnabled_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UENetSubsystem::execIsRetryEnabled)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(bool*)Z_Param__Result=P_THIS->IsRetryEnabled();
	P_NATIVE_END;
}
// End Class UENetSubsystem Function IsRetryEnabled

// Begin Class UENetSubsystem Function SetConnectTimeout
struct Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics
{
	struct ENetSubsystem_eventSetConnectTimeout_Parms
	{
		float Seconds;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FFloatPropertyParams NewProp_Seconds;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FFloatPropertyParams Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics::NewProp_Seconds = { "Seconds", nullptr, (EPropertyFlags)0x0010000000000080, UECodeGen_Private::EPropertyGenFlags::Float, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ENetSubsystem_eventSetConnectTimeout_Parms, Seconds), METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics::NewProp_Seconds,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "SetConnectTimeout", nullptr, nullptr, Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics::PropPointers), sizeof(Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics::ENetSubsystem_eventSetConnectTimeout_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics::Function_MetaDataParams), Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics::ENetSubsystem_eventSetConnectTimeout_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UENetSubsystem::execSetConnectTimeout)
{
	P_GET_PROPERTY(FFloatProperty,Z_Param_Seconds);
	P_FINISH;
	P_NATIVE_BEGIN;
	P_THIS->SetConnectTimeout(Z_Param_Seconds);
	P_NATIVE_END;
}
// End Class UENetSubsystem Function SetConnectTimeout

// Begin Class UENetSubsystem Function SetRetryEnabled
struct Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics
{
	struct ENetSubsystem_eventSetRetryEnabled_Parms
	{
		bool bEnabled;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static void NewProp_bEnabled_SetBit(void* Obj);
	static const UECodeGen_Private::FBoolPropertyParams NewProp_bEnabled;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
void Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::NewProp_bEnabled_SetBit(void* Obj)
{
	((ENetSubsystem_eventSetRetryEnabled_Parms*)Obj)->bEnabled = 1;
}
const UECodeGen_Private::FBoolPropertyParams Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::NewProp_bEnabled = { "bEnabled", nullptr, (EPropertyFlags)0x0010000000000080, UECodeGen_Private::EPropertyGenFlags::Bool | UECodeGen_Private::EPropertyGenFlags::NativeBool, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, sizeof(bool), sizeof(ENetSubsystem_eventSetRetryEnabled_Parms), &Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::NewProp_bEnabled_SetBit, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::NewProp_bEnabled,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "SetRetryEnabled", nullptr, nullptr, Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::PropPointers), sizeof(Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::ENetSubsystem_eventSetRetryEnabled_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::Function_MetaDataParams), Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::ENetSubsystem_eventSetRetryEnabled_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UENetSubsystem::execSetRetryEnabled)
{
	P_GET_UBOOL(Z_Param_bEnabled);
	P_FINISH;
	P_NATIVE_BEGIN;
	P_THIS->SetRetryEnabled(Z_Param_bEnabled);
	P_NATIVE_END;
}
// End Class UENetSubsystem Function SetRetryEnabled

// Begin Class UENetSubsystem Function SetRetryInterval
struct Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics
{
	struct ENetSubsystem_eventSetRetryInterval_Parms
	{
		float Seconds;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FFloatPropertyParams NewProp_Seconds;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FFloatPropertyParams Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics::NewProp_Seconds = { "Seconds", nullptr, (EPropertyFlags)0x0010000000000080, UECodeGen_Private::EPropertyGenFlags::Float, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ENetSubsystem_eventSetRetryInterval_Parms, Seconds), METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics::NewProp_Seconds,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UENetSubsystem, nullptr, "SetRetryInterval", nullptr, nullptr, Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics::PropPointers), sizeof(Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics::ENetSubsystem_eventSetRetryInterval_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics::Function_MetaDataParams), Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics::ENetSubsystem_eventSetRetryInterval_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UENetSubsystem_SetRetryInterval()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UENetSubsystem_SetRetryInterval_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UENetSubsystem::execSetRetryInterval)
{
	P_GET_PROPERTY(FFloatProperty,Z_Param_Seconds);
	P_FINISH;
	P_NATIVE_BEGIN;
	P_THIS->SetRetryInterval(Z_Param_Seconds);
	P_NATIVE_END;
}
// End Class UENetSubsystem Function SetRetryInterval

// Begin Class UENetSubsystem
void UENetSubsystem::StaticRegisterNativesUENetSubsystem()
{
	UClass* Class = UENetSubsystem::StaticClass();
	static const FNameNativePtrPair Funcs[] = {
		{ "Connect", &UENetSubsystem::execConnect },
		{ "Disconnect", &UENetSubsystem::execDisconnect },
		{ "GetConnectionStatus", &UENetSubsystem::execGetConnectionStatus },
		{ "GetConnectTimeout", &UENetSubsystem::execGetConnectTimeout },
		{ "GetRetryInterval", &UENetSubsystem::execGetRetryInterval },
		{ "IsConnected", &UENetSubsystem::execIsConnected },
		{ "IsConnecting", &UENetSubsystem::execIsConnecting },
		{ "IsRetryEnabled", &UENetSubsystem::execIsRetryEnabled },
		{ "SetConnectTimeout", &UENetSubsystem::execSetConnectTimeout },
		{ "SetRetryEnabled", &UENetSubsystem::execSetRetryEnabled },
		{ "SetRetryInterval", &UENetSubsystem::execSetRetryInterval },
	};
	FNativeFunctionRegistrar::RegisterFunctions(Class, Funcs, UE_ARRAY_COUNT(Funcs));
}
IMPLEMENT_CLASS_NO_AUTO_REGISTRATION(UENetSubsystem);
UClass* Z_Construct_UClass_UENetSubsystem_NoRegister()
{
	return UENetSubsystem::StaticClass();
}
struct Z_Construct_UClass_UENetSubsystem_Statics
{
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Class_MetaDataParams[] = {
		{ "DisplayName", "ENetSubSystem" },
		{ "IncludePath", "ENetSubsystem.h" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
	static constexpr UECodeGen_Private::FMetaDataPairParam NewProp_OnConnectionError_MetaData[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
	static constexpr UECodeGen_Private::FMetaDataPairParam NewProp_OnDisconnected_MetaData[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
	static constexpr UECodeGen_Private::FMetaDataPairParam NewProp_OnDataReceived_MetaData[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
	static constexpr UECodeGen_Private::FMetaDataPairParam NewProp_OnConnect_MetaData[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
	static constexpr UECodeGen_Private::FMetaDataPairParam NewProp_OnConnectDenied_MetaData[] = {
		{ "Category", "UDP" },
		{ "ModuleRelativePath", "Public/ENetSubsystem.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FMulticastDelegatePropertyParams NewProp_OnConnectionError;
	static const UECodeGen_Private::FMulticastDelegatePropertyParams NewProp_OnDisconnected;
	static const UECodeGen_Private::FMulticastDelegatePropertyParams NewProp_OnDataReceived;
	static const UECodeGen_Private::FMulticastDelegatePropertyParams NewProp_OnConnect;
	static const UECodeGen_Private::FMulticastDelegatePropertyParams NewProp_OnConnectDenied;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static UObject* (*const DependentSingletons[])();
	static constexpr FClassFunctionLinkInfo FuncInfo[] = {
		{ &Z_Construct_UFunction_UENetSubsystem_Connect, "Connect" }, // 2883334771
		{ &Z_Construct_UFunction_UENetSubsystem_Disconnect, "Disconnect" }, // 213792398
		{ &Z_Construct_UFunction_UENetSubsystem_GetConnectionStatus, "GetConnectionStatus" }, // 930312277
		{ &Z_Construct_UFunction_UENetSubsystem_GetConnectTimeout, "GetConnectTimeout" }, // 1943508927
		{ &Z_Construct_UFunction_UENetSubsystem_GetRetryInterval, "GetRetryInterval" }, // 872171506
		{ &Z_Construct_UFunction_UENetSubsystem_IsConnected, "IsConnected" }, // 1958953777
		{ &Z_Construct_UFunction_UENetSubsystem_IsConnecting, "IsConnecting" }, // 741839691
		{ &Z_Construct_UFunction_UENetSubsystem_IsRetryEnabled, "IsRetryEnabled" }, // 1481059677
		{ &Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature, "OnConnect__DelegateSignature" }, // 2955365490
		{ &Z_Construct_UDelegateFunction_UENetSubsystem_OnConnectDenied__DelegateSignature, "OnConnectDenied__DelegateSignature" }, // 1960637503
		{ &Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature, "OnDataReceived__DelegateSignature" }, // 1269094543
		{ &Z_Construct_UDelegateFunction_UENetSubsystem_OnDisconnected__DelegateSignature, "OnDisconnected__DelegateSignature" }, // 4269319058
		{ &Z_Construct_UDelegateFunction_UENetSubsystem_OnUDPConnectionError__DelegateSignature, "OnUDPConnectionError__DelegateSignature" }, // 3679118282
		{ &Z_Construct_UFunction_UENetSubsystem_SetConnectTimeout, "SetConnectTimeout" }, // 2561690599
		{ &Z_Construct_UFunction_UENetSubsystem_SetRetryEnabled, "SetRetryEnabled" }, // 742065820
		{ &Z_Construct_UFunction_UENetSubsystem_SetRetryInterval, "SetRetryInterval" }, // 381137233
	};
	static_assert(UE_ARRAY_COUNT(FuncInfo) < 2048);
	static constexpr FCppClassTypeInfoStatic StaticCppClassTypeInfo = {
		TCppClassTypeTraits<UENetSubsystem>::IsAbstract,
	};
	static const UECodeGen_Private::FClassParams ClassParams;
};
const UECodeGen_Private::FMulticastDelegatePropertyParams Z_Construct_UClass_UENetSubsystem_Statics::NewProp_OnConnectionError = { "OnConnectionError", nullptr, (EPropertyFlags)0x0010000010080000, UECodeGen_Private::EPropertyGenFlags::InlineMulticastDelegate, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(UENetSubsystem, OnConnectionError), Z_Construct_UDelegateFunction_UENetSubsystem_OnUDPConnectionError__DelegateSignature, METADATA_PARAMS(UE_ARRAY_COUNT(NewProp_OnConnectionError_MetaData), NewProp_OnConnectionError_MetaData) }; // 3679118282
const UECodeGen_Private::FMulticastDelegatePropertyParams Z_Construct_UClass_UENetSubsystem_Statics::NewProp_OnDisconnected = { "OnDisconnected", nullptr, (EPropertyFlags)0x0010000010080000, UECodeGen_Private::EPropertyGenFlags::InlineMulticastDelegate, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(UENetSubsystem, OnDisconnected), Z_Construct_UDelegateFunction_UENetSubsystem_OnDisconnected__DelegateSignature, METADATA_PARAMS(UE_ARRAY_COUNT(NewProp_OnDisconnected_MetaData), NewProp_OnDisconnected_MetaData) }; // 4269319058
const UECodeGen_Private::FMulticastDelegatePropertyParams Z_Construct_UClass_UENetSubsystem_Statics::NewProp_OnDataReceived = { "OnDataReceived", nullptr, (EPropertyFlags)0x0010000010080000, UECodeGen_Private::EPropertyGenFlags::InlineMulticastDelegate, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(UENetSubsystem, OnDataReceived), Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature, METADATA_PARAMS(UE_ARRAY_COUNT(NewProp_OnDataReceived_MetaData), NewProp_OnDataReceived_MetaData) }; // 1269094543
const UECodeGen_Private::FMulticastDelegatePropertyParams Z_Construct_UClass_UENetSubsystem_Statics::NewProp_OnConnect = { "OnConnect", nullptr, (EPropertyFlags)0x0010000010080000, UECodeGen_Private::EPropertyGenFlags::InlineMulticastDelegate, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(UENetSubsystem, OnConnect), Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature, METADATA_PARAMS(UE_ARRAY_COUNT(NewProp_OnConnect_MetaData), NewProp_OnConnect_MetaData) }; // 2955365490
const UECodeGen_Private::FMulticastDelegatePropertyParams Z_Construct_UClass_UENetSubsystem_Statics::NewProp_OnConnectDenied = { "OnConnectDenied", nullptr, (EPropertyFlags)0x0010000010080000, UECodeGen_Private::EPropertyGenFlags::InlineMulticastDelegate, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(UENetSubsystem, OnConnectDenied), Z_Construct_UDelegateFunction_UENetSubsystem_OnConnectDenied__DelegateSignature, METADATA_PARAMS(UE_ARRAY_COUNT(NewProp_OnConnectDenied_MetaData), NewProp_OnConnectDenied_MetaData) }; // 1960637503
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UClass_UENetSubsystem_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_UENetSubsystem_Statics::NewProp_OnConnectionError,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_UENetSubsystem_Statics::NewProp_OnDisconnected,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_UENetSubsystem_Statics::NewProp_OnDataReceived,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_UENetSubsystem_Statics::NewProp_OnConnect,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_UENetSubsystem_Statics::NewProp_OnConnectDenied,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UClass_UENetSubsystem_Statics::PropPointers) < 2048);
UObject* (*const Z_Construct_UClass_UENetSubsystem_Statics::DependentSingletons[])() = {
	(UObject* (*)())Z_Construct_UClass_UGameInstanceSubsystem,
	(UObject* (*)())Z_Construct_UPackage__Script_ToS_Network,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UClass_UENetSubsystem_Statics::DependentSingletons) < 16);
const UECodeGen_Private::FClassParams Z_Construct_UClass_UENetSubsystem_Statics::ClassParams = {
	&UENetSubsystem::StaticClass,
	nullptr,
	&StaticCppClassTypeInfo,
	DependentSingletons,
	FuncInfo,
	Z_Construct_UClass_UENetSubsystem_Statics::PropPointers,
	nullptr,
	UE_ARRAY_COUNT(DependentSingletons),
	UE_ARRAY_COUNT(FuncInfo),
	UE_ARRAY_COUNT(Z_Construct_UClass_UENetSubsystem_Statics::PropPointers),
	0,
	0x009000A0u,
	METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UClass_UENetSubsystem_Statics::Class_MetaDataParams), Z_Construct_UClass_UENetSubsystem_Statics::Class_MetaDataParams)
};
UClass* Z_Construct_UClass_UENetSubsystem()
{
	if (!Z_Registration_Info_UClass_UENetSubsystem.OuterSingleton)
	{
		UECodeGen_Private::ConstructUClass(Z_Registration_Info_UClass_UENetSubsystem.OuterSingleton, Z_Construct_UClass_UENetSubsystem_Statics::ClassParams);
	}
	return Z_Registration_Info_UClass_UENetSubsystem.OuterSingleton;
}
template<> TOS_NETWORK_API UClass* StaticClass<UENetSubsystem>()
{
	return UENetSubsystem::StaticClass();
}
UENetSubsystem::UENetSubsystem() {}
DEFINE_VTABLE_PTR_HELPER_CTOR(UENetSubsystem);
UENetSubsystem::~UENetSubsystem() {}
// End Class UENetSubsystem

// Begin Registration
struct Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_Statics
{
	static constexpr FClassRegisterCompiledInInfo ClassInfo[] = {
		{ Z_Construct_UClass_UENetSubsystem, UENetSubsystem::StaticClass, TEXT("UENetSubsystem"), &Z_Registration_Info_UClass_UENetSubsystem, CONSTRUCT_RELOAD_VERSION_INFO(FClassReloadVersionInfo, sizeof(UENetSubsystem), 3705910108U) },
	};
};
static FRegisterCompiledInInfo Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_2100696214(TEXT("/Script/ToS_Network"),
	Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_Statics::ClassInfo, UE_ARRAY_COUNT(Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ENetSubsystem_h_Statics::ClassInfo),
	nullptr, 0,
	nullptr, 0);
// End Registration
PRAGMA_ENABLE_DEPRECATION_WARNINGS
