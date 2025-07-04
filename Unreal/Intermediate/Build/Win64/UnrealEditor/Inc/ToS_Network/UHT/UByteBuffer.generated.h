// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

// IWYU pragma: private, include "UByteBuffer.h"
#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
class UByteBuffer;
#ifdef TOS_NETWORK_UByteBuffer_generated_h
#error "UByteBuffer.generated.h already included, missing '#pragma once' in UByteBuffer.h"
#endif
#define TOS_NETWORK_UByteBuffer_generated_h

#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h_33_RPC_WRAPPERS_NO_PURE_DECLS \
	DECLARE_FUNCTION(execReadInt64); \
	DECLARE_FUNCTION(execReadInt32); \
	DECLARE_FUNCTION(execReadByte); \
	DECLARE_FUNCTION(execWriteInt64); \
	DECLARE_FUNCTION(execWriteInt32); \
	DECLARE_FUNCTION(execWriteByte); \
	DECLARE_FUNCTION(execCreateByteBuffer); \
	DECLARE_FUNCTION(execCreateEmptyByteBuffer);


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h_33_INCLASS_NO_PURE_DECLS \
private: \
	static void StaticRegisterNativesUByteBuffer(); \
	friend struct Z_Construct_UClass_UByteBuffer_Statics; \
public: \
	DECLARE_CLASS(UByteBuffer, UObject, COMPILED_IN_FLAGS(0), CASTCLASS_None, TEXT("/Script/ToS_Network"), NO_API) \
	DECLARE_SERIALIZER(UByteBuffer)


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h_33_ENHANCED_CONSTRUCTORS \
	/** Standard constructor, called after all reflected properties have been initialized */ \
	NO_API UByteBuffer(const FObjectInitializer& ObjectInitializer = FObjectInitializer::Get()); \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	UByteBuffer(UByteBuffer&&); \
	UByteBuffer(const UByteBuffer&); \
public: \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, UByteBuffer); \
	DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(UByteBuffer); \
	DEFINE_DEFAULT_OBJECT_INITIALIZER_CONSTRUCTOR_CALL(UByteBuffer) \
	NO_API virtual ~UByteBuffer();


#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h_30_PROLOG
#define FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h_33_GENERATED_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h_33_RPC_WRAPPERS_NO_PURE_DECLS \
	FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h_33_INCLASS_NO_PURE_DECLS \
	FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h_33_ENHANCED_CONSTRUCTORS \
private: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


template<> TOS_NETWORK_API UClass* StaticClass<class UByteBuffer>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h


PRAGMA_ENABLE_DEPRECATION_WARNINGS
