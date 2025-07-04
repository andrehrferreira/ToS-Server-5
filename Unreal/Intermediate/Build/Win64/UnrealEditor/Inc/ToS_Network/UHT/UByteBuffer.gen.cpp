// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/GeneratedCppIncludes.h"
#include "ToS_Network/Public/UByteBuffer.h"
PRAGMA_DISABLE_DEPRECATION_WARNINGS
void EmptyLinkFunctionForGeneratedCodeUByteBuffer() {}

// Begin Cross Module References
COREUOBJECT_API UClass* Z_Construct_UClass_UObject();
TOS_NETWORK_API UClass* Z_Construct_UClass_UByteBuffer();
TOS_NETWORK_API UClass* Z_Construct_UClass_UByteBuffer_NoRegister();
UPackage* Z_Construct_UPackage__Script_ToS_Network();
// End Cross Module References

// Begin Class UByteBuffer Function CreateByteBuffer
struct Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics
{
	struct ByteBuffer_eventCreateByteBuffer_Parms
	{
		TArray<uint8> Data;
		UByteBuffer* ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "ByteBuffer" },
		{ "ModuleRelativePath", "Public/UByteBuffer.h" },
	};
	static constexpr UECodeGen_Private::FMetaDataPairParam NewProp_Data_MetaData[] = {
		{ "NativeConst", "" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FBytePropertyParams NewProp_Data_Inner;
	static const UECodeGen_Private::FArrayPropertyParams NewProp_Data;
	static const UECodeGen_Private::FObjectPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FBytePropertyParams Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::NewProp_Data_Inner = { "Data", nullptr, (EPropertyFlags)0x0000000000000000, UECodeGen_Private::EPropertyGenFlags::Byte, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, 0, nullptr, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FArrayPropertyParams Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::NewProp_Data = { "Data", nullptr, (EPropertyFlags)0x0010000008000182, UECodeGen_Private::EPropertyGenFlags::Array, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventCreateByteBuffer_Parms, Data), EArrayPropertyFlags::None, METADATA_PARAMS(UE_ARRAY_COUNT(NewProp_Data_MetaData), NewProp_Data_MetaData) };
const UECodeGen_Private::FObjectPropertyParams Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventCreateByteBuffer_Parms, ReturnValue), Z_Construct_UClass_UByteBuffer_NoRegister, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::NewProp_Data_Inner,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::NewProp_Data,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UByteBuffer, nullptr, "CreateByteBuffer", nullptr, nullptr, Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::PropPointers), sizeof(Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::ByteBuffer_eventCreateByteBuffer_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x14422401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::Function_MetaDataParams), Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::ByteBuffer_eventCreateByteBuffer_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UByteBuffer_CreateByteBuffer()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UByteBuffer_CreateByteBuffer_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UByteBuffer::execCreateByteBuffer)
{
	P_GET_TARRAY_REF(uint8,Z_Param_Out_Data);
	P_FINISH;
	P_NATIVE_BEGIN;
	*(UByteBuffer**)Z_Param__Result=UByteBuffer::CreateByteBuffer(Z_Param_Out_Data);
	P_NATIVE_END;
}
// End Class UByteBuffer Function CreateByteBuffer

// Begin Class UByteBuffer Function CreateEmptyByteBuffer
struct Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics
{
	struct ByteBuffer_eventCreateEmptyByteBuffer_Parms
	{
		int32 Capacity;
		UByteBuffer* ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "ByteBuffer" },
		{ "CPP_Default_Capacity", "3600" },
		{ "ModuleRelativePath", "Public/UByteBuffer.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FIntPropertyParams NewProp_Capacity;
	static const UECodeGen_Private::FObjectPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FIntPropertyParams Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::NewProp_Capacity = { "Capacity", nullptr, (EPropertyFlags)0x0010000000000080, UECodeGen_Private::EPropertyGenFlags::Int, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventCreateEmptyByteBuffer_Parms, Capacity), METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FObjectPropertyParams Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventCreateEmptyByteBuffer_Parms, ReturnValue), Z_Construct_UClass_UByteBuffer_NoRegister, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::NewProp_Capacity,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UByteBuffer, nullptr, "CreateEmptyByteBuffer", nullptr, nullptr, Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::PropPointers), sizeof(Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::ByteBuffer_eventCreateEmptyByteBuffer_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x14022401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::Function_MetaDataParams), Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::ByteBuffer_eventCreateEmptyByteBuffer_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UByteBuffer::execCreateEmptyByteBuffer)
{
	P_GET_PROPERTY(FIntProperty,Z_Param_Capacity);
	P_FINISH;
	P_NATIVE_BEGIN;
	*(UByteBuffer**)Z_Param__Result=UByteBuffer::CreateEmptyByteBuffer(Z_Param_Capacity);
	P_NATIVE_END;
}
// End Class UByteBuffer Function CreateEmptyByteBuffer

// Begin Class UByteBuffer Function ReadByte
struct Z_Construct_UFunction_UByteBuffer_ReadByte_Statics
{
	struct ByteBuffer_eventReadByte_Parms
	{
		uint8 ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "ByteBuffer" },
		{ "ModuleRelativePath", "Public/UByteBuffer.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FBytePropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FBytePropertyParams Z_Construct_UFunction_UByteBuffer_ReadByte_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Byte, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventReadByte_Parms, ReturnValue), nullptr, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UByteBuffer_ReadByte_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_ReadByte_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_ReadByte_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UByteBuffer_ReadByte_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UByteBuffer, nullptr, "ReadByte", nullptr, nullptr, Z_Construct_UFunction_UByteBuffer_ReadByte_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_ReadByte_Statics::PropPointers), sizeof(Z_Construct_UFunction_UByteBuffer_ReadByte_Statics::ByteBuffer_eventReadByte_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_ReadByte_Statics::Function_MetaDataParams), Z_Construct_UFunction_UByteBuffer_ReadByte_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UByteBuffer_ReadByte_Statics::ByteBuffer_eventReadByte_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UByteBuffer_ReadByte()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UByteBuffer_ReadByte_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UByteBuffer::execReadByte)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(uint8*)Z_Param__Result=P_THIS->ReadByte();
	P_NATIVE_END;
}
// End Class UByteBuffer Function ReadByte

// Begin Class UByteBuffer Function ReadInt32
struct Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics
{
	struct ByteBuffer_eventReadInt32_Parms
	{
		int32 ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "ByteBuffer" },
		{ "ModuleRelativePath", "Public/UByteBuffer.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FIntPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FIntPropertyParams Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Int, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventReadInt32_Parms, ReturnValue), METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UByteBuffer, nullptr, "ReadInt32", nullptr, nullptr, Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics::PropPointers), sizeof(Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics::ByteBuffer_eventReadInt32_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics::Function_MetaDataParams), Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics::ByteBuffer_eventReadInt32_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UByteBuffer_ReadInt32()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UByteBuffer_ReadInt32_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UByteBuffer::execReadInt32)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(int32*)Z_Param__Result=P_THIS->ReadInt32();
	P_NATIVE_END;
}
// End Class UByteBuffer Function ReadInt32

// Begin Class UByteBuffer Function ReadInt64
struct Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics
{
	struct ByteBuffer_eventReadInt64_Parms
	{
		int64 ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "ByteBuffer" },
		{ "ModuleRelativePath", "Public/UByteBuffer.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FInt64PropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FInt64PropertyParams Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Int64, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventReadInt64_Parms, ReturnValue), METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UByteBuffer, nullptr, "ReadInt64", nullptr, nullptr, Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics::PropPointers), sizeof(Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics::ByteBuffer_eventReadInt64_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics::Function_MetaDataParams), Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics::ByteBuffer_eventReadInt64_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UByteBuffer_ReadInt64()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UByteBuffer_ReadInt64_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UByteBuffer::execReadInt64)
{
	P_FINISH;
	P_NATIVE_BEGIN;
	*(int64*)Z_Param__Result=P_THIS->ReadInt64();
	P_NATIVE_END;
}
// End Class UByteBuffer Function ReadInt64

// Begin Class UByteBuffer Function WriteByte
struct Z_Construct_UFunction_UByteBuffer_WriteByte_Statics
{
	struct ByteBuffer_eventWriteByte_Parms
	{
		uint8 Value;
		UByteBuffer* ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "ByteBuffer" },
		{ "ModuleRelativePath", "Public/UByteBuffer.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FBytePropertyParams NewProp_Value;
	static const UECodeGen_Private::FObjectPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FBytePropertyParams Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::NewProp_Value = { "Value", nullptr, (EPropertyFlags)0x0010000000000080, UECodeGen_Private::EPropertyGenFlags::Byte, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventWriteByte_Parms, Value), nullptr, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FObjectPropertyParams Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventWriteByte_Parms, ReturnValue), Z_Construct_UClass_UByteBuffer_NoRegister, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::NewProp_Value,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UByteBuffer, nullptr, "WriteByte", nullptr, nullptr, Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::PropPointers), sizeof(Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::ByteBuffer_eventWriteByte_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::Function_MetaDataParams), Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::ByteBuffer_eventWriteByte_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UByteBuffer_WriteByte()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UByteBuffer_WriteByte_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UByteBuffer::execWriteByte)
{
	P_GET_PROPERTY(FByteProperty,Z_Param_Value);
	P_FINISH;
	P_NATIVE_BEGIN;
	*(UByteBuffer**)Z_Param__Result=P_THIS->WriteByte(Z_Param_Value);
	P_NATIVE_END;
}
// End Class UByteBuffer Function WriteByte

// Begin Class UByteBuffer Function WriteInt32
struct Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics
{
	struct ByteBuffer_eventWriteInt32_Parms
	{
		int32 Value;
		UByteBuffer* ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "ByteBuffer" },
		{ "ModuleRelativePath", "Public/UByteBuffer.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FIntPropertyParams NewProp_Value;
	static const UECodeGen_Private::FObjectPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FIntPropertyParams Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::NewProp_Value = { "Value", nullptr, (EPropertyFlags)0x0010000000000080, UECodeGen_Private::EPropertyGenFlags::Int, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventWriteInt32_Parms, Value), METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FObjectPropertyParams Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventWriteInt32_Parms, ReturnValue), Z_Construct_UClass_UByteBuffer_NoRegister, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::NewProp_Value,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UByteBuffer, nullptr, "WriteInt32", nullptr, nullptr, Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::PropPointers), sizeof(Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::ByteBuffer_eventWriteInt32_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::Function_MetaDataParams), Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::ByteBuffer_eventWriteInt32_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UByteBuffer_WriteInt32()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UByteBuffer_WriteInt32_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UByteBuffer::execWriteInt32)
{
	P_GET_PROPERTY(FIntProperty,Z_Param_Value);
	P_FINISH;
	P_NATIVE_BEGIN;
	*(UByteBuffer**)Z_Param__Result=P_THIS->WriteInt32(Z_Param_Value);
	P_NATIVE_END;
}
// End Class UByteBuffer Function WriteInt32

// Begin Class UByteBuffer Function WriteInt64
struct Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics
{
	struct ByteBuffer_eventWriteInt64_Parms
	{
		int64 Value;
		UByteBuffer* ReturnValue;
	};
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Function_MetaDataParams[] = {
		{ "Category", "ByteBuffer" },
		{ "ModuleRelativePath", "Public/UByteBuffer.h" },
	};
#endif // WITH_METADATA
	static const UECodeGen_Private::FInt64PropertyParams NewProp_Value;
	static const UECodeGen_Private::FObjectPropertyParams NewProp_ReturnValue;
	static const UECodeGen_Private::FPropertyParamsBase* const PropPointers[];
	static const UECodeGen_Private::FFunctionParams FuncParams;
};
const UECodeGen_Private::FInt64PropertyParams Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::NewProp_Value = { "Value", nullptr, (EPropertyFlags)0x0010000000000080, UECodeGen_Private::EPropertyGenFlags::Int64, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventWriteInt64_Parms, Value), METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FObjectPropertyParams Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::NewProp_ReturnValue = { "ReturnValue", nullptr, (EPropertyFlags)0x0010000000000580, UECodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, nullptr, nullptr, 1, STRUCT_OFFSET(ByteBuffer_eventWriteInt64_Parms, ReturnValue), Z_Construct_UClass_UByteBuffer_NoRegister, METADATA_PARAMS(0, nullptr) };
const UECodeGen_Private::FPropertyParamsBase* const Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::PropPointers[] = {
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::NewProp_Value,
	(const UECodeGen_Private::FPropertyParamsBase*)&Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::NewProp_ReturnValue,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::PropPointers) < 2048);
const UECodeGen_Private::FFunctionParams Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UByteBuffer, nullptr, "WriteInt64", nullptr, nullptr, Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::PropPointers, UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::PropPointers), sizeof(Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::ByteBuffer_eventWriteInt64_Parms), RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04020401, 0, 0, METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::Function_MetaDataParams), Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::Function_MetaDataParams) };
static_assert(sizeof(Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::ByteBuffer_eventWriteInt64_Parms) < MAX_uint16);
UFunction* Z_Construct_UFunction_UByteBuffer_WriteInt64()
{
	static UFunction* ReturnFunction = nullptr;
	if (!ReturnFunction)
	{
		UECodeGen_Private::ConstructUFunction(&ReturnFunction, Z_Construct_UFunction_UByteBuffer_WriteInt64_Statics::FuncParams);
	}
	return ReturnFunction;
}
DEFINE_FUNCTION(UByteBuffer::execWriteInt64)
{
	P_GET_PROPERTY(FInt64Property,Z_Param_Value);
	P_FINISH;
	P_NATIVE_BEGIN;
	*(UByteBuffer**)Z_Param__Result=P_THIS->WriteInt64(Z_Param_Value);
	P_NATIVE_END;
}
// End Class UByteBuffer Function WriteInt64

// Begin Class UByteBuffer
void UByteBuffer::StaticRegisterNativesUByteBuffer()
{
	UClass* Class = UByteBuffer::StaticClass();
	static const FNameNativePtrPair Funcs[] = {
		{ "CreateByteBuffer", &UByteBuffer::execCreateByteBuffer },
		{ "CreateEmptyByteBuffer", &UByteBuffer::execCreateEmptyByteBuffer },
		{ "ReadByte", &UByteBuffer::execReadByte },
		{ "ReadInt32", &UByteBuffer::execReadInt32 },
		{ "ReadInt64", &UByteBuffer::execReadInt64 },
		{ "WriteByte", &UByteBuffer::execWriteByte },
		{ "WriteInt32", &UByteBuffer::execWriteInt32 },
		{ "WriteInt64", &UByteBuffer::execWriteInt64 },
	};
	FNativeFunctionRegistrar::RegisterFunctions(Class, Funcs, UE_ARRAY_COUNT(Funcs));
}
IMPLEMENT_CLASS_NO_AUTO_REGISTRATION(UByteBuffer);
UClass* Z_Construct_UClass_UByteBuffer_NoRegister()
{
	return UByteBuffer::StaticClass();
}
struct Z_Construct_UClass_UByteBuffer_Statics
{
#if WITH_METADATA
	static constexpr UECodeGen_Private::FMetaDataPairParam Class_MetaDataParams[] = {
		{ "IncludePath", "UByteBuffer.h" },
		{ "ModuleRelativePath", "Public/UByteBuffer.h" },
	};
#endif // WITH_METADATA
	static UObject* (*const DependentSingletons[])();
	static constexpr FClassFunctionLinkInfo FuncInfo[] = {
		{ &Z_Construct_UFunction_UByteBuffer_CreateByteBuffer, "CreateByteBuffer" }, // 2841146180
		{ &Z_Construct_UFunction_UByteBuffer_CreateEmptyByteBuffer, "CreateEmptyByteBuffer" }, // 595542561
		{ &Z_Construct_UFunction_UByteBuffer_ReadByte, "ReadByte" }, // 1401600177
		{ &Z_Construct_UFunction_UByteBuffer_ReadInt32, "ReadInt32" }, // 2379851759
		{ &Z_Construct_UFunction_UByteBuffer_ReadInt64, "ReadInt64" }, // 2120529790
		{ &Z_Construct_UFunction_UByteBuffer_WriteByte, "WriteByte" }, // 445616411
		{ &Z_Construct_UFunction_UByteBuffer_WriteInt32, "WriteInt32" }, // 1431573304
		{ &Z_Construct_UFunction_UByteBuffer_WriteInt64, "WriteInt64" }, // 493447519
	};
	static_assert(UE_ARRAY_COUNT(FuncInfo) < 2048);
	static constexpr FCppClassTypeInfoStatic StaticCppClassTypeInfo = {
		TCppClassTypeTraits<UByteBuffer>::IsAbstract,
	};
	static const UECodeGen_Private::FClassParams ClassParams;
};
UObject* (*const Z_Construct_UClass_UByteBuffer_Statics::DependentSingletons[])() = {
	(UObject* (*)())Z_Construct_UClass_UObject,
	(UObject* (*)())Z_Construct_UPackage__Script_ToS_Network,
};
static_assert(UE_ARRAY_COUNT(Z_Construct_UClass_UByteBuffer_Statics::DependentSingletons) < 16);
const UECodeGen_Private::FClassParams Z_Construct_UClass_UByteBuffer_Statics::ClassParams = {
	&UByteBuffer::StaticClass,
	nullptr,
	&StaticCppClassTypeInfo,
	DependentSingletons,
	FuncInfo,
	nullptr,
	nullptr,
	UE_ARRAY_COUNT(DependentSingletons),
	UE_ARRAY_COUNT(FuncInfo),
	0,
	0,
	0x001000A0u,
	METADATA_PARAMS(UE_ARRAY_COUNT(Z_Construct_UClass_UByteBuffer_Statics::Class_MetaDataParams), Z_Construct_UClass_UByteBuffer_Statics::Class_MetaDataParams)
};
UClass* Z_Construct_UClass_UByteBuffer()
{
	if (!Z_Registration_Info_UClass_UByteBuffer.OuterSingleton)
	{
		UECodeGen_Private::ConstructUClass(Z_Registration_Info_UClass_UByteBuffer.OuterSingleton, Z_Construct_UClass_UByteBuffer_Statics::ClassParams);
	}
	return Z_Registration_Info_UClass_UByteBuffer.OuterSingleton;
}
template<> TOS_NETWORK_API UClass* StaticClass<UByteBuffer>()
{
	return UByteBuffer::StaticClass();
}
UByteBuffer::UByteBuffer(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {}
DEFINE_VTABLE_PTR_HELPER_CTOR(UByteBuffer);
UByteBuffer::~UByteBuffer() {}
// End Class UByteBuffer

// Begin Registration
struct Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h_Statics
{
	static constexpr FClassRegisterCompiledInInfo ClassInfo[] = {
		{ Z_Construct_UClass_UByteBuffer, UByteBuffer::StaticClass, TEXT("UByteBuffer"), &Z_Registration_Info_UClass_UByteBuffer, CONSTRUCT_RELOAD_VERSION_INFO(FClassReloadVersionInfo, sizeof(UByteBuffer), 2850293897U) },
	};
};
static FRegisterCompiledInInfo Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h_3729101135(TEXT("/Script/ToS_Network"),
	Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h_Statics::ClassInfo, UE_ARRAY_COUNT(Z_CompiledInDeferFile_FID_NetworkSample_Plugins_ToS_Network_Source_ToS_Network_Public_UByteBuffer_h_Statics::ClassInfo),
	nullptr, 0,
	nullptr, 0);
// End Registration
PRAGMA_ENABLE_DEPRECATION_WARNINGS
