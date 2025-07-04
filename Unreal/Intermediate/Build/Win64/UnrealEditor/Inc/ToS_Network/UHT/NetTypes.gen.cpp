// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/GeneratedCppIncludes.h"
#include "ToS_Network/Public/NetTypes.h"
PRAGMA_DISABLE_DEPRECATION_WARNINGS
void EmptyLinkFunctionForGeneratedCodeNetTypes() {}

// Begin Cross Module References
TOS_NETWORK_API UEnum* Z_Construct_UEnum_ToS_Network_EConnectionStatus();
TOS_NETWORK_API UEnum* Z_Construct_UEnum_ToS_Network_EPacketType();
UPackage* Z_Construct_UPackage__Script_ToS_Network();
// End Cross Module References

// Begin Enum EConnectionStatus
static FEnumRegistrationInfo Z_Registration_Info_UEnum_EConnectionStatus;
static UEnum* EConnectionStatus_StaticEnum()
{
	if (!Z_Registration_Info_UEnum_EConnectionStatus.OuterSingleton)
	{
		Z_Registration_Info_UEnum_EConnectionStatus.OuterSingleton = GetStaticEnum(Z_Construct_UEnum_ToS_Network_EConnectionStatus, (UObject*)Z_Construct_UPackage__Script_ToS_Network(), TEXT("EConnectionStatus"));
	}
	return Z_Registration_Info_UEnum_EConnectionStatus.OuterSingleton;
}
template<> TOS_NETWORK_API UEnum* StaticEnum<EConnectionStatus>()
{
	return EConnectionStatus_StaticEnum();
}
struct Z_Construct_UEnum_ToS_Network_EConnectionStatus_Statics
{
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Enum_MetaDataParams[] = {
		{ "BlueprintType", "true" },
		{ "Connected.DisplayName", "Connected" },
		{ "Connected.Name", "EConnectionStatus::Connected" },
		{ "Connecting.DisplayName", "Connecting" },
		{ "Connecting.Name", "EConnectionStatus::Connecting" },
		{ "ConnectionFailed.DisplayName", "Connection Failed" },
		{ "ConnectionFailed.Name", "EConnectionStatus::ConnectionFailed" },
		{ "Disconnected.DisplayName", "Disconnected" },
		{ "Disconnected.Name", "EConnectionStatus::Disconnected" },
		{ "ModuleRelativePath", "Public/NetTypes.h" },
	};
#endif // WITH_METADATA
	static constexpr UECodeGen_Private::FEnumeratorParam Enumerators[] = {
		{ "EConnectionStatus::Disconnected", (int64)EConnectionStatus::Disconnected },
		{ "EConnectionStatus::Connecting", (int64)EConnectionStatus::Connecting },
		{ "EConnectionStatus::Connected", (int64)EConnectionStatus::Connected },
		{ "EConnectionStatus::ConnectionFailed", (int64)EConnectionStatus::ConnectionFailed },
	};
	static const UECodeGen_Private::FEnumParams EnumParams;
};
const UECodeGen_Private::FEnumParams Z_Construct_UEnum_ToS_Network_EConnectionStatus_Statics::EnumParams = {
	(UObject*(*)())Z_Construct_UPackage__Script_ToS_Network,
	nullptr,
	"EConnectionStatus",
	"EConnectionStatus",
	Z_Construct_UEnum_ToS_Network_EConnectionStatus_Statics::Enumerators,
	RF_Public|RF_Transient|RF_MarkAsNative,
	UE_ARRAY_COUNT(Z_Construct_UEnum_ToS_Network_EConnectionStatus_Statics::Enumerators),
	EEnumFlags::None,
	(uint8)UEnum::ECppForm::EnumClass,
	METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UEnum_ToS_Network_EConnectionStatus_Statics::Enum_MetaDataParams), Z_Construct_UEnum_ToS_Network_EConnectionStatus_Statics::Enum_MetaDataParams)
};
UEnum* Z_Construct_UEnum_ToS_Network_EConnectionStatus()
{
	if (!Z_Registration_Info_UEnum_EConnectionStatus.InnerSingleton)
	{
		UECodeGen_Private::ConstructUEnum(Z_Registration_Info_UEnum_EConnectionStatus.InnerSingleton, Z_Construct_UEnum_ToS_Network_EConnectionStatus_Statics::EnumParams);
	}
	return Z_Registration_Info_UEnum_EConnectionStatus.InnerSingleton;
}
// End Enum EConnectionStatus

// Begin Enum EPacketType
static FEnumRegistrationInfo Z_Registration_Info_UEnum_EPacketType;
static UEnum* EPacketType_StaticEnum()
{
	if (!Z_Registration_Info_UEnum_EPacketType.OuterSingleton)
	{
		Z_Registration_Info_UEnum_EPacketType.OuterSingleton = GetStaticEnum(Z_Construct_UEnum_ToS_Network_EPacketType, (UObject*)Z_Construct_UPackage__Script_ToS_Network(), TEXT("EPacketType"));
	}
	return Z_Registration_Info_UEnum_EPacketType.OuterSingleton;
}
template<> TOS_NETWORK_API UEnum* StaticEnum<EPacketType>()
{
	return EPacketType_StaticEnum();
}
struct Z_Construct_UEnum_ToS_Network_EPacketType_Statics
{
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Enum_MetaDataParams[] = {
		{ "Ack.DisplayName", "Ack" },
		{ "Ack.Name", "EPacketType::Ack" },
		{ "BlueprintType", "true" },
		{ "Connect.DisplayName", "Connect" },
		{ "Connect.Name", "EPacketType::Connect" },
		{ "ConnectionAccepted.DisplayName", "ConnectionAccepted" },
		{ "ConnectionAccepted.Name", "EPacketType::ConnectionAccepted" },
		{ "ConnectionDenied.DisplayName", "ConnectionDenied" },
		{ "ConnectionDenied.Name", "EPacketType::ConnectionDenied" },
		{ "Disconnect.DisplayName", "Disconnect" },
		{ "Disconnect.Name", "EPacketType::Disconnect" },
		{ "Error.DisplayName", "Error" },
		{ "Error.Name", "EPacketType::Error" },
		{ "ModuleRelativePath", "Public/NetTypes.h" },
		{ "Ping.DisplayName", "Ping" },
		{ "Ping.Name", "EPacketType::Ping" },
		{ "Pong.DisplayName", "Pong" },
		{ "Pong.Name", "EPacketType::Pong" },
		{ "Reliable.DisplayName", "Reliable" },
		{ "Reliable.Name", "EPacketType::Reliable" },
		{ "Unreliable.DisplayName", "Unreliable" },
		{ "Unreliable.Name", "EPacketType::Unreliable" },
	};
#endif // WITH_METADATA
	static constexpr UECodeGen_Private::FEnumeratorParam Enumerators[] = {
		{ "EPacketType::Connect", (int64)EPacketType::Connect },
		{ "EPacketType::Ping", (int64)EPacketType::Ping },
		{ "EPacketType::Pong", (int64)EPacketType::Pong },
		{ "EPacketType::Reliable", (int64)EPacketType::Reliable },
		{ "EPacketType::Unreliable", (int64)EPacketType::Unreliable },
		{ "EPacketType::Ack", (int64)EPacketType::Ack },
		{ "EPacketType::Disconnect", (int64)EPacketType::Disconnect },
		{ "EPacketType::Error", (int64)EPacketType::Error },
		{ "EPacketType::ConnectionDenied", (int64)EPacketType::ConnectionDenied },
		{ "EPacketType::ConnectionAccepted", (int64)EPacketType::ConnectionAccepted },
	};
	static const UECodeGen_Private::FEnumParams EnumParams;
};
const UECodeGen_Private::FEnumParams Z_Construct_UEnum_ToS_Network_EPacketType_Statics::EnumParams = {
	(UObject*(*)())Z_Construct_UPackage__Script_ToS_Network,
	nullptr,
	"EPacketType",
	"EPacketType",
	Z_Construct_UEnum_ToS_Network_EPacketType_Statics::Enumerators,
	RF_Public|RF_Transient|RF_MarkAsNative,
	UE_ARRAY_COUNT(Z_Construct_UEnum_ToS_Network_EPacketType_Statics::Enumerators),
	EEnumFlags::None,
	(uint8)UEnum::ECppForm::EnumClass,
	METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UEnum_ToS_Network_EPacketType_Statics::Enum_MetaDataParams), Z_Construct_UEnum_ToS_Network_EPacketType_Statics::Enum_MetaDataParams)
};
UEnum* Z_Construct_UEnum_ToS_Network_EPacketType()
{
	if (!Z_Registration_Info_UEnum_EPacketType.InnerSingleton)
	{
		UECodeGen_Private::ConstructUEnum(Z_Registration_Info_UEnum_EPacketType.InnerSingleton, Z_Construct_UEnum_ToS_Network_EPacketType_Statics::EnumParams);
	}
	return Z_Registration_Info_UEnum_EPacketType.InnerSingleton;
}
// End Enum EPacketType

// Begin Registration
struct Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_NetTypes_h_Statics
{
	static constexpr FEnumRegisterCompiledInInfo EnumInfo[] = {
		{ EConnectionStatus_StaticEnum, TEXT("EConnectionStatus"), &Z_Registration_Info_UEnum_EConnectionStatus, CONSTRUCT_RELOAD_VERSION_INFO(FEnumReloadVersionInfo, 2044594972U) },
		{ EPacketType_StaticEnum, TEXT("EPacketType"), &Z_Registration_Info_UEnum_EPacketType, CONSTRUCT_RELOAD_VERSION_INFO(FEnumReloadVersionInfo, 740466487U) },
	};
};
static FRegisterCompiledInInfo Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_NetTypes_h_1937361746(TEXT("/Script/ToS_Network"),
	nullptr, 0,
	nullptr, 0,
	Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_NetTypes_h_Statics::EnumInfo, UE_ARRAY_COUNT(Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_NetTypes_h_Statics::EnumInfo));
// End Registration
PRAGMA_ENABLE_DEPRECATION_WARNINGS
