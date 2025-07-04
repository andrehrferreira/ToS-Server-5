// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

// IWYU pragma: private, include "ToS_GameInstance.h"
#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
#ifdef TOS_NETWORK_ToS_GameInstance_generated_h
#error "ToS_GameInstance.generated.h already included, missing '#pragma once' in ToS_GameInstance.h"
#endif
#define TOS_NETWORK_ToS_GameInstance_generated_h

#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h_11_CALLBACK_WRAPPERS
#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h_11_INCLASS_NO_PURE_DECLS \
private: \
	static void StaticRegisterNativesUTOSGameInstance(); \
	friend struct Z_Construct_UClass_UTOSGameInstance_Statics; \
public: \
	DECLARE_CLASS(UTOSGameInstance, UGameInstance, COMPILED_IN_FLAGS(0 | CLASS_Transient), CASTCLASS_None, TEXT("/Script/ToS_Network"), NO_API) \
	DECLARE_SERIALIZER(UTOSGameInstance)


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h_11_ENHANCED_CONSTRUCTORS \
	/** Standard constructor, called after all reflected properties have been initialized */ \
	NO_API UTOSGameInstance(const FObjectInitializer& ObjectInitializer = FObjectInitializer::Get()); \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	UTOSGameInstance(UTOSGameInstance&&); \
	UTOSGameInstance(const UTOSGameInstance&); \
public: \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, UTOSGameInstance); \
	DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(UTOSGameInstance); \
	DEFINE_DEFAULT_OBJECT_INITIALIZER_CONSTRUCTOR_CALL(UTOSGameInstance) \
	NO_API virtual ~UTOSGameInstance();


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h_8_PROLOG
#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h_11_GENERATED_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h_11_CALLBACK_WRAPPERS \
	FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h_11_INCLASS_NO_PURE_DECLS \
	FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h_11_ENHANCED_CONSTRUCTORS \
private: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


template<> TOS_NETWORK_API UClass* StaticClass<class UTOSGameInstance>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_ToS_GameInstance_h


PRAGMA_ENABLE_DEPRECATION_WARNINGS
