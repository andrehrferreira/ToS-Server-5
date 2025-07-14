using System.Reflection;

public class UnrealTranspiler : AbstractTranspiler
{
    public static void Generate()
    {
        var contracts = GetContracts();
        string projectDirectory = GetProjectDirectory();
        string publicDir = Path.Combine(projectDirectory, "Unreal", "Source", "ToS_Network", "Public");

        if (!Directory.Exists(publicDir))
            Directory.CreateDirectory(publicDir);

        List<string> serverPackets = new List<string>();
        List<string> clientPackets = new List<string>();

        foreach (var contract in contracts)
        {
            var attribute = contract.GetCustomAttribute<ContractAttribute>();
            var fields = contract.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var structName = contract.Name;
            var rawName = structName.Replace("Packet", "");

            if (attribute.LayerType == PacketLayerType.Server)
                serverPackets.Add(rawName);
            else
                clientPackets.Add(rawName);

            GenerateHeader(publicDir, structName, rawName, fields, attribute);
        }

        GenerateEnum(publicDir, "ClientPackets", clientPackets);
        GenerateEnum(publicDir, "ServerPackets", serverPackets);
    }

    private static void GenerateEnum(string directory, string enumName, List<string> values)
    {
        string filePath = Path.Combine(directory, "Network", $"{enumName}.h");
        using var writer = new StreamWriter(filePath);

        writer.WriteLine("#pragma once");
        writer.WriteLine();
        writer.WriteLine("#include \"CoreMinimal.h\"");
        writer.WriteLine();
        writer.WriteLine($"enum class {enumName} : uint16");
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

        writer.WriteLine("#pragma once");
        writer.WriteLine();
        writer.WriteLine("#include \"CoreMinimal.h\"");
        writer.WriteLine("#include \"Network/UDPClient.h\"");
        writer.WriteLine("#include \"Network/UFlatBuffer.h\"");
        writer.WriteLine();
        writer.WriteLine($"struct {structName}");
        writer.WriteLine("{");

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<ContractFieldAttribute>();
            string type = MapType(attr.Type);
            writer.WriteLine($"    {type} {field.Name};");
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
        writer.WriteLine("    void Serialize(UFlatBuffer* Buffer) const;");
        writer.WriteLine("    void Deserialize(UFlatBuffer* Buffer);");
        writer.WriteLine("};");

        writer.WriteLine();
        writer.WriteLine($"inline void {structName}::Serialize(UFlatBuffer* Buffer) const");
        writer.WriteLine("{");

        if (attribute.PacketType != PacketType.None)
            writer.WriteLine($"    Buffer->WriteByte(static_cast<uint8>(EPacketType::{attribute.PacketType}));");
        else
        {
            if (attribute.Flags.HasFlag(ContractPacketFlags.Reliable))
                writer.WriteLine("    Buffer->WriteByte(static_cast<uint8>(EPacketType::Reliable));");
            else
                writer.WriteLine("    Buffer->WriteByte(static_cast<uint8>(EPacketType::Unreliable));");
            writer.WriteLine($"    Buffer->WriteUInt16(static_cast<uint16>(ServerPacket::{rawName}));");
        }

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<ContractFieldAttribute>();
            string name = field.Name;
            writer.WriteLine(GetSerializeLine(attr.Type, name));
        }

        writer.WriteLine("}");
        writer.WriteLine();
        writer.WriteLine($"inline void {structName}::Deserialize(UFlatBuffer* Buffer)");
        writer.WriteLine("{");

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<ContractFieldAttribute>();
            string name = field.Name;
            writer.WriteLine(GetDeserializeLine(attr.Type, name));
        }

        writer.WriteLine("}");
    }

    private static string MapType(string type) => type switch
    {
        "integer" or "int" or "int32" => "int32",
        "uint" => "uint32",
        "ushort" => "uint16",
        "short" => "int16",
        "byte" => "uint8",
        "float" or "decimal" => "float",
        "bool" or "boolean" => "bool",
        "long" => "int64",
        "ulong" => "uint64",
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
        "integer" or "int" or "int32" => $"    Buffer->WriteInt32({name});",
        "uint" => $"    Buffer->WriteUInt32({name});",
        "ushort" => $"    Buffer->WriteUInt16({name});",
        "short" => $"    Buffer->WriteInt16({name});",
        "byte" => $"    Buffer->WriteByte({name});",
        "float" => $"    Buffer->WriteFloat({name});",
        "long" => $"    Buffer->WriteInt64({name});",
        "ulong" => $"    Buffer->WriteVarULong({name});",
        "bool" or "boolean" => $"    Buffer->WriteBool({name});",
        "decimal" => $"    Buffer->WriteFloat({name});",
        "FVector" => $"    Buffer->Write<FVector>({name});",
        "FRotator" => $"    Buffer->Write<FRotator>({name});",
        "id" => $"    Buffer->WriteInt32(UBase36::Base36ToInt({name}));",
        "str" or "string" => $"    Buffer->WriteString({name});",
        _ => $"    // Unsupported type: {type}",
    };

    private static string GetDeserializeLine(string type, string name) => type switch
    {
        "integer" or "int" or "int32" => $"    {name} = Buffer->ReadInt32();",
        "uint" => $"    {name} = Buffer->ReadUInt32();",
        "ushort" => $"    {name} = Buffer->ReadUInt16();",
        "short" => $"    {name} = Buffer->ReadInt16();",
        "byte" => $"    {name} = Buffer->ReadByte();",
        "float" => $"    {name} = Buffer->ReadFloat();",
        "long" => $"    {name} = Buffer->ReadInt64();",
        "ulong" => $"    {name} = Buffer->ReadVarULong();",
        "bool" or "boolean" => $"    {name} = Buffer->ReadBool();",
        "decimal" => $"    {name} = Buffer->ReadFloat();",
        "FVector" => $"    {name} = Buffer->Read<FVector>();",
        "FRotator" => $"    {name} = Buffer->Read<FRotator>();",
        "id" => $"    {name} = UBase36::IntToBase36(Buffer->ReadInt32());",
        "str" or "string" => $"    {name} = Buffer->ReadString();",
        _ => $"    // Unsupported type: {type}",
    };
}
