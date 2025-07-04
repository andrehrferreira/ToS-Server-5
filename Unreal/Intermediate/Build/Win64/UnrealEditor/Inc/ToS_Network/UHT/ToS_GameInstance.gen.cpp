// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/GeneratedCppIncludes.h"
#include "ToS_Network/Public/ToS_GameInstance.h"
PRAGMA_DISABLE_DEPRECATION_WARNINGS
void EmptyLinkFunctionForGeneratedCodeToS_GameInstance() {}

// Begin Cross Module References
ENGINE_API UClass* Z_Construct_UClass_UGameInstance();
TOS_NETWORK_API UClass* Z_Construct_UClass_UTOSGameInstance();
TOS_NETWORK_API UClass* Z_Construct_UClass_UTOSGameInstance_NoRegister();
UPackage* Z_Construct_UPackage__Script_ToS_Network();
// End Cross Module References

// Begin Class UTOSGameInstance Function OnGameInstanceStarted
static const FName NAME_UTOSGameInstance_OnGameInstanceStarted = FName(TEXT("OnGameInstanceStarted"));
void UTOSGameInstance::OnGameInstanceStarted()
{
	UFunction* Func = FindFunctionChecked(NAME_UTOSGameInstance_OnGameInstanceStarted);
	ProcessEvent(Func,NULL);
}
struct Z_Construct_UFunction_UTOSGameInstance_OnGameInstanceStarted_Statics
{
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "GameInstance" },
		{ "ModuleRelativePath", "Public/ToS_GameInstance.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UTOSGameInstance_OnGameInstanceStarted_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UTOSGameInstance, nullptr, "OnGameInstanceStarted", nullptr, nullptr, nullptr, 0, 0, RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x08020800, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UTOSGameInstance_OnGameInstanceStarted_Statics::Function_MetaDataParams), Z_Construct_UFunction_UTOSGameInstance_OnGameInstanceStarted_Statics::Function_MetaDataParams) };
UFunction* Z_Construct_UFunction_UTOSGameInstance_OnGameInstanceStarted()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UTOSGameInstance_OnGameInstanceStarted_Statics::FuncParams);
	}
	return ReturnFunction;
}
// End Class UTOSGameInstance Function OnGameInstanceStarted

// Begin Class UTOSGameInstance
void UTOSGameInstance::StaticRegisterNativesUTOSGameInstance()
{
}
IMPLEMENT_CLASS_NO_AUTO_REGISTRATION(UTOSGameInstance);
UClass* Z_Construct_UClass_UTOSGameInstance_NoRegister()
{
	return UTOSGameInstance::StaticClass();
}
struct Z_Construct_UClass_UTOSGameInstance_Statics
{
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Class_MetaDataParams[] = {
		{ "IncludePath", "ToS_GameInstance.h" },
		{ "ModuleRelativePath", "Public/ToS_GameInstance.h" },
	};
	static constexpr UECodeGen_Private::FMetaDataPairParam NewProp_ServerAutoConnect_MetaData[] = {
		{ "Category", "Network" },
		{ "ModuleRelativePath", "Public/ToS_GameInstance.h" },
	};
	static constexpr UECodeGen_Private::FMetaDataPairParam NewProp_ServerIP_MetaData[] = {
		{ "Category", "Network" },
		{ "ModuleRelativePath", "Public/ToS_GameInstance.h" },
	};
	static constexpr UECodeGen_Private::FMetaDataPairParam NewProp_ServerPort_MetaData[] = {
		{ "Category", "Network" },
		{ "ModuleRelativePath", "Public/ToS_GameInstance.h" },
	};
#endif // WITH_METADATA
	static void NewProp_ServerAutoConnect_SetBit(void* Obj);
	static const UECodeGen_Private::FBoolPropertyParams NewProp_ServerAutoConnect;
	static const UECodeGen_Private::FStrPropertyParams NewProp_ServerIP;
	static const UECodeGen_Private::FIntPropertyParams NewProp_ServerPort;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static UObject* (*const DependentSingletons[])();
	static constexpr FClassFunctionLinkInfo FuncInfo[] = {
		{ &Z_Construct_UFunction_UTOSGameInstance_OnGameInstanceStarted, "OnGameInstanceStarted" }, // 2461105966
	};
	static_assert(UE_ARRAY_COUNT(FuncInfo) < 2048);
	static constexpr FCppClassTypeInfoStatic StaticCppClassTypeInfo = {
		TCppClassTypeTraits<UTOSGameInstance>::IsAbstract,
	};
	static const UECodeGen_Private::FClassParams ClassParams;
};
void Z_Construct_UClass_UTOSGameInstance_Statics::NewProp_ServerAutoConnect_SetBit(void* Obj)
{
	((UTOSGameInstance*)Obj)->ServerAutoConnect = 1;
}
const UECodeGen_Private::FBoolPropertyParams Z_Construct_UClass_UTOSGameInstance_Statics::NewProp_ServerAutoConnect = { "ServerAutoConnect", nullptr, (EPropertyFlags)0x0010000000000005, UECodeGen_Private::EPropertyGenFlags::Bool | UECodeGen_Private::EPropertyGenFlags::NativeBool, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, sizeof(bool), sizeof(UTOSGameInstance), &Z_Construct_UClass_UTOSGameInstance_Statics::NewProp_ServerAutoConnect_SetBit, METADATA_PARAMS(UE_ARRAY_COUNT(NewProp_ServerAutoConnect_MetaData), NewProp_ServerAutoConnect_MetaData) };
const UECodeGen_Private::FStrPropertyParams Z_Construct_UClass_UTOSGameInstance_Statics::NewProp_ServerIP = { "ServerIP", nullptr, (EPropertyFlags)0x0010000000000005, UECodeGen_Private::EPropertyGenFlags::Str, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(UTOSGameInstance, ServerIP), METADATA_PARAMS(UE_ARRAY_COUNT(NewProp_ServerIP_MetaData), NewProp_ServerIP_MetaData) };
const UECodeGen_Private::FIntPropertyParams Z_Construct_UClass_UTOSGameInstance_Statics::NewProp_ServerPort = { "ServerPort", nullptr, (EPropertyFlags)0x0010000000000005, UECodeGen_Private::EPropertyGenFlags::Int, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(UTOSGameInstance, ServerPort), METADATA_PARAMS(UE_ARRAY_COUNT(NewProp_ServerPort_MetaData), NewProp_ServerPort_MetaData) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UClass_UTOSGameInstance_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_UTOSGameInstance_Statics::NewProp_ServerAutoConnect,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_UTOSGameInstance_Statics::NewProp_ServerIP,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_UTOSGameInstance_Statics::NewProp_ServerPort,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UClass_UTOSGameInstance_Statics::PropPointers) < 2048);
UObject* (*const Z_Construct_UClass_UTOSGameInstance_Statics::DependentSingletons[])() = {
	(UObject* (*)())Z_Construct_UClass_UGameInstance,
	(UObject* (*)())Z_Construct_UPackage__Script_ToS_Network,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UClass_UTOSGameInstance_Statics::DependentSingletons) < 16);
const UECodeGen_Private::FClassParams Z_Construct_UClass_UTOSGameInstance_Statics::ClassParams = {
	&UTOSGameInstance::StaticClass,
	nullptr,
	&StaticCppClassTypeInfo,
	DependentSingletons,
	FuncInfo,
	Z_Construct_UClass_UTOSGameInstance_Statics::PropPointers,
	nullptr,
	UE_ARRAY_COUNT(DependentSingletons),
	UE_ARRAY_COUNT(FuncInfo),
	UE_ARRAY_COUNT(Z_Construct_UClass_UTOSGameInstance_Statics::PropPointers),
	0,
	0x009000A8u,
	METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UClass_UTOSGameInstance_Statics::Class_MetaDataParams), Z_Construct_UClass_UTOSGameInstance_Statics::Class_MetaDataParams)
};
UClass* Z_Construct_UClass_UTOSGameInstance()
{
	if (!Z_Registration_Info_UClass_UTOSGameInstance.OuterSingleton)
	{
		UECodeGen_Private::ConstructUClass(Z_Registration_Info_UClass_UTOSGameInstance.OuterSingleton, Z_Construct_UClass_UTOSGameInstance_Statics::ClassParams);
	}
	return Z_Registration_Info_UClass_UTOSGameInstance.OuterSingleton;
}
template<> TOS_NETWORK_API UClass* StaticClass<UTOSGameInstance>()
{
	return UTOSGameInstance::StaticClass();
}
UTOSGameInstance::UTOSGameInstance(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {}
DEFINE_VTABLE_PTR_HELPER_CTOR(UTOSGameInstance);
UTOSGameInstance::~UTOSGameInstance() {}
// End Class UTOSGameInstance

// Begin Registration
struct Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h_Statics
{
	static constexpr FClassRegisterCompiledInInfo ClassInfo[] = {
		{ Z_Construct_UClass_UTOSGameInstance, UTOSGameInstance::StaticClass, TEXT("UTOSGameInstance"), &Z_Registration_Info_UClass_UTOSGameInstance, CONSTRUCT_RELOAD_VERSION_INFO(FClassReloadVersionInfo, sizeof(UTOSGameInstance), 2289031422U) },
	};
};
static FRegisterCompiledInInfo Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h_388066839(TEXT("/Script/ToS_Network"),
	Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h_Statics::ClassInfo, UE_ARRAY_COUNT(Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h_Statics::ClassInfo),
	nullptr, 0,
	nullptr, 0);
// End Registration
PRAGMA_ENABLE_DEPRECATION_WARNINGS
