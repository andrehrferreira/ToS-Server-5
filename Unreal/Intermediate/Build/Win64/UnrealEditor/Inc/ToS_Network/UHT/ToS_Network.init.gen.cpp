// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/GeneratedCppIncludes.h"
PRAGMA_DISABLE_DEPRECATION_WARNINGS
void EmptyLinkFunctionForGeneratedCodeToS_Network_init() {}
	TOS_NETWORK_API UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature();
	TOS_NETWORK_API UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnConnectDenied__DelegateSignature();
	TOS_NETWORK_API UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature();
	TOS_NETWORK_API UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnDisconnected__DelegateSignature();
	TOS_NETWORK_API UFunction* Z_Construct_UDelegateFunction_UENetSubsystem_OnUDPConnectionError__DelegateSignature();
	static FPackageRegistrationInfo Z_Registration_Info_UPackage__Script_ToS_Network;
	FORCENOINLINE UPackage* Z_Construct_UPackage__Script_ToS_Network()
	{
		if (!Z_Registration_Info_UPackage__Script_ToS_Network.OuterSingleton)
		{
			static UObject* (*const SingletonFuncArray[])() = {
				(UObject* (*)())Z_Construct_UDelegateFunction_UENetSubsystem_OnConnect__DelegateSignature,
				(UObject* (*)())Z_Construct_UDelegateFunction_UENetSubsystem_OnConnectDenied__DelegateSignature,
				(UObject* (*)())Z_Construct_UDelegateFunction_UENetSubsystem_OnDataReceived__DelegateSignature,
				(UObject* (*)())Z_Construct_UDelegateFunction_UENetSubsystem_OnDisconnected__DelegateSignature,
				(UObject* (*)())Z_Construct_UDelegateFunction_UENetSubsystem_OnUDPConnectionError__DelegateSignature,
			};
			static const UECodeGen_Private::FPackageParams PackageParams = {
				"/Script/ToS_Network",
				SingletonFuncArray,
				UE_ARRAY_COUNT(SingletonFuncArray),
				PKG_CompiledIn | 0x00000000,
				0xA5608C8A,
				0x60E7F27A,
				METADATA_PARAMS(0, nullptr)
			};
			UECodeGen_Private::ConstructUPackage(Z_Registration_Info_UPackage__Script_ToS_Network.OuterSingleton, PackageParams);
		}
		return Z_Registration_Info_UPackage__Script_ToS_Network.OuterSingleton;
	}
	static FRegisterCompiledInInfo Z_CompiledInDeferPackage_UPackage__Script_ToS_Network(Z_Construct_UPackage__Script_ToS_Network, TEXT("/Script/ToS_Network"), Z_Registration_Info_UPackage__Script_ToS_Network, CONSTRUCT_RELOAD_VERSION_INFO(FPackageReloadVersionInfo, 0xA5608C8A, 0x60E7F27A));
PRAGMA_ENABLE_DEPRECATION_WARNINGS
