/*
 * UnrealTraspiler
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

using System.Reflection;

public class UnrealTraspiler : AbstractTranspiler
{
    private static List<string> clientPackets = new List<string>();
    private static List<string> serverPackets = new List<string>();
    private static List<string> multiplexPackets = new List<string>();

    public static void Generate()
    {
        var contracts = GetContracts();

        string projectDirectory = GetProjectDirectory();
        string unrealPath = Path.Combine(projectDirectory, "Unreal", "Source", "ToS_Network";

        foreach (var contract in contracts)
        {
            var fields = contract.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var attribute = contract.GetCustomAttribute<ContractAttribute>();

            string headerPath = Path.Combine(unrealPath, "Public", "Packets");
            string cppPath = Path.Combine(unrealPath, "Private", "Packets");

            if (!Directory.Exists(headerPath))
                Directory.CreateDirectory(headerPath);

            if (!Directory.Exists(cppPath))
                Directory.CreateDirectory(cppPath);

            string headerFilePath = Path.Combine(unrealPath, "Public", "Packets", $"{contract.Name}Packet.h");
            string cppFilePath = Path.Combine(unrealPath, "Private", "Packets", $"{contract.Name}Packet.cpp");

            using (var writer = new StreamWriter(headerFilePath))
            {
                GenerateHeaderFile(writer, contract, fields);
            }

            using (var writer = new StreamWriter(cppFilePath))
            {
                GenerateCppFile(writer, contract, fields);
            }

            switch (attribute.LayerType)
            {
                case PacketLayerType.Client: clientPackets.Add(contract.Name); break;
                case PacketLayerType.Server: serverPackets.Add(contract.Name); break;
            }
        }

        clientPackets.AddRange(multiplexPackets);
        serverPackets.AddRange(multiplexPackets);

        GenerateEnumHeader("ClientPacket", clientPackets, unrealPath);
        GenerateEnumHeader("ServerPacket", serverPackets, unrealPath);
    }

    private static void GenerateHeaderFile(StreamWriter writer, Type contract, FieldInfo[] fields)
    {
        if (fields.Length > 0)
        {
            var contractName = contract.Name.Replace("DTO", "");

            writer.WriteLine("// This file was generated automatically, please do not change it.");
            writer.WriteLine();
            writer.WriteLine("#pragma once");
            writer.WriteLine();
            writer.WriteLine("#include \"CoreMinimal.h\"");
            writer.WriteLine("#include \"Network/UFlatBuffer.h\"");
            writer.WriteLine($"#include \"{contractName}Packet.generated.h\"");
            writer.WriteLine();
            writer.WriteLine("USTRUCT(BlueprintType)");
            writer.WriteLine($"struct F{contractName}Recive");
            writer.WriteLine("{");
            writer.WriteLine("    GENERATED_USTRUCT_BODY();");
            writer.WriteLine();

            foreach (var field in fields)
            {
                var attributeField = field.GetCustomAttribute<ContractFieldAttribute>();
                if (attributeField != null)
                {
                    var fieldType = ConvertToUnrealType(attributeField.Type);
                    writer.WriteLine($"    UPROPERTY(EditAnywhere, BlueprintReadWrite)");
                    writer.WriteLine($"    {fieldType} {field.Name};");
                    writer.WriteLine();
                }
            }

            writer.WriteLine("};");

            writer.WriteLine();
            writer.WriteLine("USTRUCT(BlueprintType)");
            writer.WriteLine($"struct F{contractName}Send");
            writer.WriteLine("{");
            writer.WriteLine("    GENERATED_USTRUCT_BODY();");
            writer.WriteLine();

            foreach (var field in fields)
            {
                var attributeField = field.GetCustomAttribute<ContractFieldAttribute>();
                if (attributeField != null)
                {
                    var fieldType = ConvertToUnrealType(attributeField.Type);
                    writer.WriteLine($"    UPROPERTY(EditAnywhere, BlueprintReadWrite)");
                    writer.WriteLine($"    {fieldType} {field.Name};");
                    writer.WriteLine();
                }
            }
            writer.WriteLine("};");

            writer.WriteLine();
            writer.WriteLine("// Function class to serialize and deserialize struct F" + contractName);
            writer.WriteLine("UCLASS()");
            writer.WriteLine($"class U{contractName}Library : public UBlueprintFunctionLibrary");
            writer.WriteLine("{");
            writer.WriteLine("    GENERATED_BODY()");
            writer.WriteLine();
            writer.WriteLine("public:");
            writer.WriteLine();
            writer.WriteLine($"    UFUNCTION(BlueprintCallable, Category = \"{contractName}Serialization\")");
            writer.WriteLine($"    static F{contractName}Recive {contractName}Deserialize(UByteBuffer* Buffer);");
            writer.WriteLine();
            writer.WriteLine($"    UFUNCTION(BlueprintCallable, Category = \"{contractName}Serialization\")");
            writer.WriteLine($"    static UByteBuffer* {contractName}Serialize(const F{contractName}Send& Data);");

            /*var attribute = contract.GetCustomAttribute<ContractAttribute>();
            if (attribute.Type == PacketType.Client || attribute.Type == PacketType.Multiplex)
            {
                writer.WriteLine();
                writer.WriteLine($"    UFUNCTION(BlueprintCallable, Category = \"{contractName}Communication\")");
                writer.WriteLine($"    void Send{contractName}(const F{contractName}& Data);");
            }*/

            writer.WriteLine("};");
        }
    }

    private static void GenerateCppFile(StreamWriter writer, Type contract, FieldInfo[] fields)
    {
        var contractName = contract.Name.Replace("DTO", "");

        if (fields.Length > 0)
        {
            writer.WriteLine("// This file was generated automatically, please do not change it.");
            writer.WriteLine();
            writer.WriteLine($"#include \"Packets/{contractName}Packet.h\"");
            writer.WriteLine("#include \"Network/UFlatBuffer.h\"");
            writer.WriteLine();
            writer.WriteLine($"F{contractName} U{contractName}Library::{contractName}Deserialize(UFlatBuffer* Buffer)");
            writer.WriteLine("{");
            writer.WriteLine($"    F{contractName}Recive Data = F{contractName}Recive();");
            writer.WriteLine("    if (!Buffer) return Data;");
            writer.WriteLine();

            foreach (var field in fields)
            {
                var attributeField = field.GetCustomAttribute<ContractFieldAttribute>();

                if (attributeField != null)
                {
                    var fieldType = attributeField.Type;
                    var fieldName = field.Name;

                    switch (fieldType)
                    {
                        case "int":
                        case "int32":
                        case "integer":
                            writer.WriteLine($"    Data.{fieldName} = Buffer->GetInt32();");
                            break;
                        case "float":
                            writer.WriteLine($"    Data.{fieldName} = Buffer->GetFloat();");
                            break;
                        case "string":
                        case "str":
                            writer.WriteLine($"    Data.{fieldName} = Buffer->GetString();");
                            break;
                        case "bool":
                            writer.WriteLine($"    Data.{fieldName} = Buffer->GetBool();");
                            break;
                        case "byte":
                            writer.WriteLine($"    Data.{fieldName} = Buffer->GetByte();");
                            break;
                        case "id":
                            writer.WriteLine($"    Data.{fieldName} = Buffer->GetId();");
                            break;
                        case "vector3":
                            writer.WriteLine($"    Data.{fieldName} = Buffer->GetVector();");
                            break;
                        case "rotator":
                            writer.WriteLine($"    Data.{fieldName} = Buffer->GetRotator();");
                            break;
                        default:
                            writer.WriteLine($"    // Unsupported type: {fieldType}");
                            break;
                    }
                }
            }

            writer.WriteLine("    return Data;");
            writer.WriteLine("}");
            writer.WriteLine();
            writer.WriteLine($"UByteBuffer* U{contractName}Library::{contractName}Serialize(const F{contractName}Send& Data)");
            writer.WriteLine("{");
            writer.WriteLine("    UByteBuffer* Buffer = UByteBuffer::CreateEmptyByteBuffer();");
            writer.WriteLine("    if (!Buffer) return nullptr;");
            writer.WriteLine();

            foreach (var field in fields)
            {
                var attributeField = field.GetCustomAttribute<ContractFieldAttribute>();

                if (attributeField != null)
                {
                    var fieldType = attributeField.Type;
                    var fieldName = field.Name;

                    switch (fieldType)
                    {
                        case "int":
                        case "int32":
                        case "integer":
                            writer.WriteLine($"    Buffer->PutInt32(Data.{fieldName});");
                            break;
                        case "float":
                            writer.WriteLine($"    Buffer->PutFloat(Data.{fieldName});");
                            break;
                        case "string":
                        case "str":
                            writer.WriteLine($"    Buffer->PutString(Data.{fieldName});");
                            break;
                        case "bool":
                            writer.WriteLine($"    Buffer->PutBool(Data.{fieldName});");
                            break;
                        case "byte":
                            writer.WriteLine($"    Buffer->PutByte(Data.{fieldName});");
                            break;
                        case "id":
                            writer.WriteLine($"    Buffer->PutId(Data.{fieldName});");
                            break;
                        case "vector3":
                            writer.WriteLine($"    Buffer->PutVector(Data.{fieldName});");
                            break;
                        case "rotator":
                            writer.WriteLine($"    Buffer->PutRotator(Data.{fieldName});");
                            break;
                        default:
                            writer.WriteLine($"    // Unsupported type: {fieldType}");
                            break;
                    }
                }
            }

            writer.WriteLine("    return Buffer;");
            writer.WriteLine("}");

            /*var attribute = contract.GetCustomAttribute<ContractAttribute>();
            if (attribute.Type == PacketType.Client || attribute.Type == PacketType.Multiplex)
            {
                writer.WriteLine();
                writer.WriteLine($"void U{contractName}Library::Send{contractName}(const F{contractName}& Data)");
                writer.WriteLine("{");
                writer.WriteLine("    if (!UServerSubsystem::GetServerSubsystem(nullptr) || !UServerSubsystem::GetServerSubsystem(nullptr)->UDPInstance) return;");
                writer.WriteLine();
                writer.WriteLine($"    UByteBuffer* Buffer = U{contractName}Library::{contractName}Serialize(Data);");
                writer.WriteLine("    if (!Buffer) return;");
                writer.WriteLine();
                writer.WriteLine($"    UServerSubsystem::GetServerSubsystem(nullptr)->UDPInstance->SendMessage(static_cast<uint8>(PacketType::{contractName}), Buffer);");
                writer.WriteLine("}");
            }*/
        }
    }

    private static void GenerateEnumHeader(string enumName, List<string> values, string unrealPath)
    {
        string directoryPath = Path.Combine(unrealPath, "Public", "Enums");
        string filePath = Path.Combine(directoryPath, $"{enumName}.h");

        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        if (values.Count > 0)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("#pragma once");
                writer.WriteLine();
                writer.WriteLine($"UENUM(BlueprintType)");
                writer.WriteLine($"enum class E{enumName} : uint8");
                writer.WriteLine("{");

                for (int i = 0; i < values.Count; i++)
                {
                    string value = values[i];

                    if (i == values.Count - 1)
                        writer.WriteLine($"    {value} UMETA(DisplayName = \"{value}\")");
                    else
                        writer.WriteLine($"    {value} UMETA(DisplayName = \"{value}\"),");
                }

                writer.WriteLine("};");
            }
        }
        else
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private static string ConvertToUnrealType(string fieldType)
    {
        return fieldType.ToLower() switch
        {
            "int32" => "int32",
            "int" => "int32",
            "integer" => "int32",
            "float" => "float",
            "str" => "FString",
            "string" => "FString",
            "id" => "FString",
            "byte" => "uint8",
            "boolean" => "bool",
            "bool" => "bool",
            "vector3" => "FVector",
            "FVector" => "FVector",
            "rotator" => "FRotator",
            "FRotator" => "FRotator",
            "buffer" => "TArray<uint8>&",
            _ => "UnsupportedType"
        };
    }
}