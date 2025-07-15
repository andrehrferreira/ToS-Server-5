using System;
using System.Reflection;
using System.Text;

public class UnrealTranspiler : AbstractTranspiler
{
    private static List<string> clientPackets = new List<string>();
    private static List<string> serverPackets = new List<string>();

    public static void Generate()
    {
        var contracts = GetContracts();
        string projectDirectory = GetProjectDirectory();
        string publicDir = Path.Combine(projectDirectory, "Unreal", "Source", "ToS_Network", "Public");

        if (!Directory.Exists(publicDir))
            Directory.CreateDirectory(publicDir);

        foreach (var contract in contracts)
        {
            var attribute = contract.GetCustomAttribute<ContractAttribute>();
            var fields = contract.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var structName = contract.Name;
            var rawName = structName.Replace("Packet", "");

            if (attribute.LayerType == PacketLayerType.Server && attribute.PacketType == PacketType.None)
                serverPackets.Add(rawName);
            else if (attribute.LayerType == PacketLayerType.Client)
                clientPackets.Add(rawName);

            GenerateHeader(publicDir, structName, rawName, fields, attribute);
        }

        GenerateEnum(publicDir, "ClientPackets", clientPackets);
        GenerateEnum(publicDir, "ServerPackets", serverPackets);
        GenerateSubsystem();
    }

    private static List<string> GetClientPackets()
    {
        return clientPackets;
    }

    private static List<string> GetServerPackets()
    {
        return serverPackets;
    }

    private static Type GetContractByName(string name)
    {
        var contracts = GetContracts();
        return contracts.FirstOrDefault(c => c.Name == name);
    }

    public static void GenerateSubsystem()
    {
        string projectDirectory = GetProjectDirectory();
        string pluginDir = Path.Combine(projectDirectory, "Unreal", "Source", "ToS_Network");
        string templateDir = Path.Combine(projectDirectory, "Templates");

        string cppFilePath = Path.Combine(templateDir, "ENetSubsystem.cpp");
        string headerFilePath = Path.Combine(templateDir, "ENetSubsystem.h");

        string headerFilePathClient = Path.Combine(pluginDir, "Public", "Network", "ENetSubsystem.h");
        string cppFilePathClient = Path.Combine(pluginDir, "Private", "Network", "ENetSubsystem.cpp");

        if (File.Exists(headerFilePath))
        {
            string headerContent = File.ReadAllText(headerFilePath);
            headerContent = headerContent.Replace("//%INCLUDES%", GenerateIncludes());
            headerContent = headerContent.Replace("//%DELEGATES%", GenerateDelegates());
            headerContent = headerContent.Replace("//%EVENTS%", GenerateEvents());
            File.WriteAllText(headerFilePathClient, headerContent);
        }

        if (File.Exists(cppFilePath))
        {
            string cppContent = File.ReadAllText(cppFilePath);
            cppContent = cppContent.Replace("//%INCLUDES%", GenerateIncludes());
            cppContent = cppContent.Replace("//%FUNCTIONS%", GenerateSendFunctions());
            cppContent = cppContent.Replace("//%DATASWITCH%", GenerateParsedDataSwitch());
            File.WriteAllText(cppFilePathClient, cppContent);
        }
    }

    private static void GenerateEnum(string directory, string enumName, List<string> values)
    {
        string filePath = Path.Combine(directory, "Network", $"{enumName}.h");
        using var writer = new StreamWriter(filePath);

        writer.WriteLine("#pragma once");
        writer.WriteLine();
        writer.WriteLine("#include \"CoreMinimal.h\"");
        writer.WriteLine();
        writer.WriteLine($"enum class E{enumName} : uint16");
        writer.WriteLine("{");
        int index = 0;

        foreach (var val in values)
        {
            writer.WriteLine($"    {val} = {index},");
            index++;
        }

        writer.WriteLine("};");
    }

    private static void GenerateHeader(string directory, string structName, string rawName, FieldInfo[] fields, ContractAttribute attribute)
    {
        string filePath = Path.Combine(directory, "Packets", $"{structName}.h");
        using var writer = new StreamWriter(filePath);

        writer.WriteLine("// This file was generated automatically, please do not change it.");
        writer.WriteLine("#pragma once");
        writer.WriteLine();
        writer.WriteLine("#include \"CoreMinimal.h\"");
        writer.WriteLine("#include \"Network/UDPClient.h\"");
        writer.WriteLine("#include \"Network/UFlatBuffer.h\"");

        if (attribute.LayerType == PacketLayerType.Server)
            writer.WriteLine("#include \"Network/ServerPackets.h\"");
        else
            writer.WriteLine("#include \"Network/ClientPackets.h\"");

        writer.WriteLine($"#include \"{structName}.generated.h\"");
        writer.WriteLine();
        writer.WriteLine("USTRUCT(BlueprintType)");
        writer.WriteLine($"struct F{structName}");
        writer.WriteLine("{");
        writer.WriteLine("    GENERATED_USTRUCT_BODY();");
        writer.WriteLine();

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<ContractFieldAttribute>();
            string type = MapType(attr.Type);
            writer.WriteLine($"    UPROPERTY(EditAnywhere, BlueprintReadWrite)");
            writer.WriteLine($"    {type} {field.Name};");
            writer.WriteLine();
        }

        writer.WriteLine();
        int totalBytes = attribute.PacketType != PacketType.None ? 1 : 3;

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<ContractFieldAttribute>();
            totalBytes += TypeSize(attr.Type);
            if (totalBytes == 3600)
                break;
        }

        writer.WriteLine($"    int32 GetSize() const {{ return {totalBytes}; }}");
        writer.WriteLine();

        if(attribute.LayerType == PacketLayerType.Client)
        {
            writer.WriteLine($"    void Serialize(UFlatBuffer* Buffer)");
            writer.WriteLine("    {");

            if (attribute.PacketType != PacketType.None)
                writer.WriteLine($"        Buffer->Write<uint8>(static_cast<uint8>(EPacketType::{attribute.PacketType}));");
            else
            {
                if (attribute.Flags.HasFlag(ContractPacketFlags.Reliable))
                    writer.WriteLine("        Buffer->Write<uint8>(static_cast<uint8>(EPacketType::Reliable));");
                else
                    writer.WriteLine("        Buffer->Write<uint8>(static_cast<uint8>(EPacketType::Unreliable));");

                if(attribute.LayerType == PacketLayerType.Server)
                    writer.WriteLine($"        Buffer->Write<uint8>(static_cast<uint8>(EServerPackets::{rawName}));");
                else
                    writer.WriteLine($"        Buffer->Write<uint16>(static_cast<uint16>(EClientPackets::{rawName}));");
            }

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<ContractFieldAttribute>();
                string name = field.Name;
                writer.WriteLine(GetSerializeLine(attr.Type, name));
            }

            writer.WriteLine("    }");
            writer.WriteLine();
        }

        if (attribute.LayerType == PacketLayerType.Server && fields.Length > 0)
        {
            writer.WriteLine($"    void Deserialize(UFlatBuffer* Buffer)");
            writer.WriteLine("    {");

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<ContractFieldAttribute>();
                string name = field.Name;
                writer.WriteLine(GetDeserializeLine(attr.Type, name));
            }

            writer.WriteLine("    }");
        }

        writer.WriteLine("};");
    }

    private static string MapType(string type) => type switch
    {
        "integer" or "int" or "int32" => "int32",
        "uint" => "int32",
        "ushort" => "int32",
        "short" => "int32",
        "byte" => "uint8",
        "float" or "decimal" => "float",
        "bool" or "boolean" => "bool",
        "long" => "int64",
        "ulong" => "int64",
        "FVector" => "FVector",
        "FRotator" => "FRotator",
        "id" or "str" or "string" => "FString",
        _ => "int32",
    };

    private static int TypeSize(string type) => type switch
    {
        "integer" or "int" or "int32" or "uint" or "float" or "decimal" or "id" => 4,
        "ushort" or "short" => 2,
        "byte" or "bool" or "boolean" => 1,
        "long" or "ulong" => 8,
        "FVector" or "FRotator" => 12,
        "str" or "string" => 3600,
        _ => 0,
    };

    private static string GetSerializeLine(string type, string name) => type switch
    {
        "integer" or "int" or "int32" => $"        Buffer->Write<int32>({name});",
        "uint" => $"        Buffer->Write<uint32>(static_cast<uint32>({name}));",
        "ushort" => $"        Buffer->Write<uint16>(static_cast<uint16>({name}));",
        "short" => $"        Buffer->Write<int16>(static_cast<int16>({name}));",
        "byte" => $"        Buffer->Write<uint8>({name});",
        "float" => $"        Buffer->Write<float>({name});",
        "long" => $"        Buffer->Write<int64>({name});",
        "ulong" => $"        Buffer->Write<int64>(static_cast<int64>({name}));",
        "bool" or "boolean" => $"        Buffer->WriteBool({name});",
        "decimal" => $"        Buffer->Write<float>({name});",
        "FVector" => $"        Buffer->Write<FVector>({name});",
        "FRotator" => $"        Buffer->Write<FRotator>({name});",
        "id" => $"        Buffer->WriteInt32(UBase36::Base36ToInt({name}));",
        "str" or "string" => $"        Buffer->WriteString({name});",
        _ => $"    // Unsupported type: {type}",
    };

    private static string GetDeserializeLine(string type, string name) => type switch
    {
        "integer" or "int" or "int32" => $"    {name} = Buffer->Read<int32>();",
        "uint" => $"        {name} = static_cast<int32>(Buffer->Read<uint32>());",
        "ushort" => $"        {name} = static_cast<int32>(Buffer->Read<uint16>());",
        "short" => $"        {name} = static_cast<int32>(Buffer->Read<int16>());",
        "byte" => $"        {name} = Buffer->Read<int8>();",
        "float" => $"        {name} = Buffer->Read<float>();",
        "long" => $"        {name} = Buffer->Read<int64>();",
        "ulong" => $"        {name} = Buffer->Read<int64>();",
        "bool" or "boolean" => $"        {name} = Buffer->ReadBool();",
        "decimal" => $"        {name} = Buffer->Read<float>();",
        "FVector" => $"        {name} = Buffer->Read<FVector>();",
        "FRotator" => $"        {name} = Buffer->Read<FRotator>();",
        "id" => $"        {name} = UBase36::IntToBase36(Buffer->ReadInt32());",
        "str" or "string" => $"        {name} = Buffer->ReadString();",
        _ => $"    // Unsupported type: {type}",
    };

    private static string GetParamCountName(int count)
    {
        return count switch
        {
            1 => "One",
            2 => "Two",
            3 => "Three",
            4 => "Four",
            5 => "Five"
        };
    }

    private static string ConvertToUnrealType(string fieldType)
    {
        return fieldType.ToLower() switch
        {
            "int32" => "int32",
            "int" => "int32",
            "uint" => "int32",
            "uint32" => "int32",
            "integer" => "int32",
            "int16" => "int32",
            "uint16" => "int32",
            "short" => "int32",
            "ushort" => "int32",
            "float" => "float",
            "str" => "FString",
            "string" => "FString",
            "id" => "FString",
            "byte" => "uint8",
            "boolean" => "bool",
            "bool" => "bool",
            "vector3" => "FVector",
            "fvector" => "FVector",
            "rotator" => "FRotator",
            "frotator" => "FRotator",
            "buffer" => "TArray<uint8>&",
            _ => "UnsupportedType"
        };
    }

    private static string GenerateIncludes()
    {
        var serverPackets = GetServerPackets();
        var clientPackets = GetClientPackets();
        StringBuilder result = new StringBuilder();

        foreach (var packet in serverPackets.Concat(clientPackets))
        {
            result.AppendLine($"#include \"Packets/{packet}Packet.h\"");
        }

        return result.ToString();
    }

    private static string GenerateDelegates()
    {
        var serverPackets = GetServerPackets();
        StringBuilder result = new StringBuilder();

        foreach (var packet in serverPackets)
        {
            var contract = GetContractByName(packet + "Packet");
            var attribute = contract.GetCustomAttribute<ContractAttribute>();

            if (attribute.LayerType == PacketLayerType.Server && attribute.PacketType == PacketType.None)
            {
                var fields = contract.GetFields(BindingFlags.Public | BindingFlags.Instance);

                if (fields.Length < 6 && fields.Length > 1)
                {
                    string paramCountName = GetParamCountName(fields.Length);
                    result.Append($"    DECLARE_DYNAMIC_MULTICAST_DELEGATE_{paramCountName}Params(F{packet}Handler, ");

                    for (int i = 0; i < fields.Length; i++)
                    {
                        var fieldType = ConvertToUnrealType(fields[i].FieldType.Name);
                        result.Append($"{fieldType}, {fields[i].Name}");

                        if (i < fields.Length - 1)
                            result.Append(", ");
                    }

                    result.AppendLine(");");
                }
                else if (fields.Length == 1)
                {
                    var fieldType = ConvertToUnrealType(fields[0].FieldType.Name);
                    result.AppendLine($"    DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(F{packet}Handler, {fieldType}, {fields[0].Name});");
                }
                else if (fields.Length == 0)
                {
                    result.AppendLine($"    DECLARE_DYNAMIC_MULTICAST_DELEGATE(F{packet}Handler);");
                }
                else
                {
                    result.AppendLine($"    DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(F{packet}Handler, F{packet}, Data);");
                }
            }                
        }

        return result.ToString();
    }

    public static string GenerateEvents()
    {
        var serverPackets = GetServerPackets();
        StringBuilder result = new StringBuilder();

        foreach (var packet in serverPackets)
        {
            result.AppendLine($"    UPROPERTY(BlueprintAssignable, meta = (DisplayName = \"On{packet}\", Keywords = \"Server Events\"), Category = \"UDP\")");
            result.AppendLine($"    F{packet}Handler On{packet};");
            result.AppendLine();
        }

        /*foreach (var packet in clientPackets)
        {
            var contract = GetContractByName(packet + "Packet");
            var fields = contract.GetFields(BindingFlags.Public | BindingFlags.Instance);

            if (fields.Length > 0)
            {
                result.AppendLine($"    UFUNCTION(BlueprintCallable, Category = \"UDP\")");
                result.AppendLine($"    void Send{packet}(const F{packet}& Data);");
                result.AppendLine();
            }
            else
            {
                result.AppendLine($"    UFUNCTION(BlueprintCallable, Category = \"UDP\")");
                result.AppendLine($"    void Send{packet}();");
                result.AppendLine();
            }
        }*/

        return result.ToString();
    }

    private static string GenerateSendFunctions()
    {
        var clientPackets = GetClientPackets();
        StringBuilder result = new StringBuilder();

        foreach (var packetName in clientPackets)
        {
            var contract = GetContractByName(packetName + "Packet");
            var fields = contract.GetFields(BindingFlags.Public | BindingFlags.Instance);

            /*if (fields.Length > 0)
            {
                result.AppendLine($"void UENetSubsystem::Send{packetName}(const F{packetName}& Data)");
                result.AppendLine("{");
                result.AppendLine("    if (UdpClient) {");
                result.AppendLine($"        UFlatBuffer* Buffer = UFlatBuffer();");
                result.AppendLine($"        {packetName}Packet.Serialize(Buffer)");
                result.AppendLine($"        UdpClient->Send(Buffer);");
                result.AppendLine("    }");
                result.AppendLine("}");
                result.AppendLine();
            }
            else
            {
                result.AppendLine($"void UENetSubsystem::Send{packetName}()");
                result.AppendLine("{");
                result.AppendLine("    if (UDPInstance) {");
                result.AppendLine($"        UFlatBuffer* Buffer = UFlatBuffer();");
                result.AppendLine($"        {packetName}Packet.Serialize(Buffer)");
                result.AppendLine($"        UdpClient->Send(Buffer);");
                result.AppendLine("    }");
                result.AppendLine("}");
                result.AppendLine();
            }*/
        }

        return result.ToString();
    }

    private static string GenerateParsedDataSwitch()
    {
        StringBuilder switchBuilder = new StringBuilder();
        var serverPackets = GetServerPackets();

        foreach (var packet in serverPackets)
        {
            var contract = GetContractByName(packet + "Packet");
            var attribute = contract.GetCustomAttribute<ContractAttribute>();

            if (attribute.LayerType == PacketLayerType.Server && attribute.PacketType == PacketType.None)
            {
                var fields = contract.GetFields(BindingFlags.Public | BindingFlags.Instance);

                switchBuilder.AppendLine($"                case EServerPackets::{packet}:");
                switchBuilder.AppendLine("                {");

                if (fields.Length == 1)
                {
                    var fieldType = ConvertToUnrealType(fields[0].FieldType.Name);
                    var fieldName = fields[0].Name;

                    switchBuilder.AppendLine($"                    F{packet}Packet f{packet} = F{packet}Packet();");
                    switchBuilder.AppendLine($"                    f{packet}.Deserialize(Buffer);");
                    switchBuilder.AppendLine($"                    On{packet}.Broadcast(f{packet}.{fieldName});");
                }
                else if (fields.Length == 0)
                {
                    switchBuilder.AppendLine($"                    On{packet}.Broadcast();");
                }
                else if (fields.Length < 6)
                {
                    switchBuilder.AppendLine($"                    F{packet}Packet f{packet} = F{packet}Packet();");
                    switchBuilder.AppendLine($"                    f{packet}.Deserialize(Buffer);");
                    var parameters = new List<string>();

                    foreach (var field in fields)
                    {
                        var fieldType = ConvertToUnrealType(field.FieldType.Name);
                        var fieldName = field.Name;
                        parameters.Add($"f{packet}.{fieldName}");
                    }

                    switchBuilder.AppendLine($"                    On{packet}.Broadcast({string.Join(", ", parameters)});");
                }
                else
                {
                    switchBuilder.AppendLine($"                    F{packet}Packet f{packet} = F{packet}Packet();");
                    switchBuilder.AppendLine($"                    f{packet}.Deserialize(Buffer);");
                    switchBuilder.AppendLine($"                    On{packet}.Broadcast(Data);");
                }

                switchBuilder.AppendLine("                    break;");
                switchBuilder.AppendLine("                }");
            }            
        }

        return switchBuilder.ToString();
    }
}
