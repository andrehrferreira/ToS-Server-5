/*
* ContractTraspiler
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

public class ContractTraspiler : AbstractTranspiler
{
    public static void Generate()
    {
        var contracts = GetContracts();

        string projectDirectory = GetProjectDirectory();
        string baseDirectoryPath = Path.Combine(projectDirectory, "Core", "Packets");
        string networkDirectoryPath = Path.Combine(projectDirectory, "Core", "Network");

        List<string> serverPackets = new List<string>();
        List<bool> serverLowLevelPacket = new List<bool>();
        List<string> serverPacketType = new List<string>();
        List<string> clientPackets = new List<string>();

        if (!Directory.Exists(baseDirectoryPath))
            Directory.CreateDirectory(baseDirectoryPath);

        if (!Directory.Exists(networkDirectoryPath))
            Directory.CreateDirectory(networkDirectoryPath);

        foreach (var contract in contracts)
        {
            var fields = contract.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var attribute = contract.GetCustomAttribute<ContractAttribute>();
            var contractName = contract.Name;
            var rawName = contractName.Replace("Contract", "");

            if (attribute.LayerType == PacketLayerType.Server)
            {
                string filePath = Path.Combine(baseDirectoryPath, $"{rawName}Packet.cs");

                serverPackets.Add(contract.Name);
                serverPacketType.Add(attribute.PacketType.ToString());
                serverLowLevelPacket.Add(attribute.PacketType != PacketType.None);

                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("// This file was generated automatically, please do not change it.");
                    writer.WriteLine();
                    writer.WriteLine("using System.Runtime.CompilerServices;");               
                    writer.WriteLine();
                    writer.WriteLine($"public class {rawName}Packet: Packet");                    
                    writer.WriteLine("{");

                    GenerateSerialize(writer, contract, fields, attribute);
                    //GenerateSendFunction(writer, contract, fields, attribute);

                    writer.WriteLine("}");
                }
            }

            if (attribute.LayerType == PacketLayerType.Client)
            {
                clientPackets.Add(contract.Name);
            }
        }

        GenerateEnum("ClientPacket", clientPackets, networkDirectoryPath, null, null);
        GenerateEnum("ServerPacket", serverPackets, networkDirectoryPath, serverLowLevelPacket, serverPacketType);
    }

    private static void GenerateEnum(
        string enumName,
        List<string> values,
        string directoryPath,
        List<bool>? serverLowLevelPacket,
        List<string>? serverPacketType
    )
    {
        string filePath = Path.Combine(directoryPath, $"{enumName}.cs");

        using (var writer = new StreamWriter(filePath))
        {
            writer.WriteLine("// This file was generated automatically, please do not change it.");
            writer.WriteLine();
            writer.WriteLine($"public enum {enumName}: ushort");
            writer.WriteLine("{");

            int pointer = 0;

            for (int i = 0; i < values.Count; i++)
            {
                string value = values[i];
                writer.WriteLine($"    {value.Replace("Contract", "")} = {pointer},");
                pointer++;
            }

            writer.WriteLine("}");

            if (enumName == "ServerPacket")
            {
                writer.WriteLine();
                writer.WriteLine("public static partial class PacketRegistration");
                writer.WriteLine("{");
                writer.WriteLine("    public static void RegisterPackets()");
                writer.WriteLine("    {");

                for (int i = 0; i < values.Count; i++)
                {
                    string value = values[i];
                    var rawName = value.Replace("Contract", "");

                    if (serverLowLevelPacket[i])
                        writer.WriteLine($"        PacketManager.Register(PacketType.{serverPacketType[i]}, new {rawName}Packet());");
                    else
                        writer.WriteLine($"        PacketManager.Register(ServerPacket.{rawName}, new {rawName}Packet());");
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
                writer.WriteLine();
            }
        }
    }

    private static void GenerateSerialize(StreamWriter writer, Type contract, FieldInfo[] fields, ContractAttribute contractAttribute)
    {
        var data = contractAttribute.Flags.HasFlag(ContractPacketFlags.NoContent) ? "" : $"{contract.Name} data";
        var rawName = contract.Name.Replace("Contract", "");

        if (fields.Length > 0)
        {
            uint totalBytes = 1;

            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<ContractFieldAttribute>();
                var fieldType = attribute.Type;
                var fieldName = field.Name;

                switch (fieldType)
                {
                    case "integer":
                    case "int":
                    case "int32":
                    case "uint":
                    case "decimal":
                    case "float":
                    case "id":
                        totalBytes += 4;
                        break;
                    case "ushort":
                    case "short":
                        totalBytes += 2;
                        break;
                    case "str":
                    case "string":
                        totalBytes = 3600;
                        break;
                    case "byte":
                    case "bool":
                    case "boolean":
                        totalBytes += 1;
                        break;
                    case "long":
                        totalBytes += 8;
                        break;
                    case "FVector":
                    case "FRotator":
                        totalBytes += 12;
                        break;
                }

                if (totalBytes == 3600)
                    break;
            }

            writer.WriteLine($"    public {rawName}Packet()");
            writer.WriteLine("    {");
            writer.WriteLine($"        _buffer = new byte[{totalBytes}];");
            writer.WriteLine("    }");
            writer.WriteLine();

            writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine($"    public override void Serialize(object? data = null)");
            writer.WriteLine("    {");
            writer.WriteLine($"        var typedData = data is {contract.Name} p ? p : throw new InvalidCastException(\"Invalid data type.\");");
            writer.WriteLine("        Serialize(typedData);");
            writer.WriteLine("    }");
            writer.WriteLine();

            writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine($"    public void Serialize({data})");
            writer.WriteLine("    {");
            writer.WriteLine("        uint offset = 0;");

            if (contractAttribute.PacketType != PacketType.None)
                writer.WriteLine($"        offset = ByteBuffer.WritePacketType(_buffer, offset, PacketType.{contractAttribute.PacketType.ToString()});");
            else
                writer.WriteLine($"        offset = ByteBuffer.WriteUShort(_buffer, offset, (ushort)ServerPacket.{contract.Name.Replace("Contract", "")});");

            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<ContractFieldAttribute>();
                var fieldType = attribute.Type;
                var fieldName = field.Name;

                switch (fieldType)
                {
                    case "integer":
                    case "int":
                    case "int32":
                        writer.WriteLine($"        offset = ByteBuffer.WriteInt(_buffer, offset, data.{fieldName});");
                        break;
                    case "uint":
                        writer.WriteLine($"        offset = ByteBuffer.WriteUInt(_buffer, offset, data.{fieldName});");
                        break;
                    case "ushort":
                        writer.WriteLine($"        offset = ByteBuffer.WriteUShort(_buffer, offset, data.{fieldName});");
                        break;
                    case "short":
                        writer.WriteLine($"        offset = ByteBuffer.WriteShort(_buffer, offset, data.{fieldName});");
                        break;
                    case "str":
                    case "string":
                        writer.WriteLine($"        offset = ByteBuffer.WriteString(_buffer, offset, data.{fieldName});");
                        break;
                    case "byte":
                        writer.WriteLine($"        offset = ByteBuffer.WriteByte(_buffer, offset, data.{fieldName});");
                        break;
                    case "float":
                        writer.WriteLine($"        offset = ByteBuffer.WriteFloat(_buffer, offset, data.{fieldName});");
                        break;
                    case "long":
                        writer.WriteLine($"        offset = ByteBuffer.WriteLong(_buffer, offset, data.{fieldName});");
                        break;
                    case "bool":
                    case "boolean":
                        writer.WriteLine($"        offset = ByteBuffer.WriteBool(_buffer, offset, data.{fieldName});");
                        break;
                    case "FVector":
                        writer.WriteLine($"        offset = ByteBuffer.WriteFVector(_buffer, offset, data.{fieldName});");
                        break;
                    case "FRotator":
                        writer.WriteLine($"        offset = ByteBuffer.WriteFRotator(_buffer, offset, data.{fieldName});");
                        break;
                    case "id":
                        writer.WriteLine($"        offset = ByteBuffer.WriteInt(_buffer, offset, Base36.ToInt(data.{fieldName}));");
                        break;
                    case "decimal":
                        writer.WriteLine($"        offset = ByteBuffer.WriteFloat(_buffer, offset, (float)data.{fieldName});");
                        break;
                    default:
                        writer.WriteLine($"    // Tipo não suportado: {fieldType}");
                        break;
                }

            }

            writer.WriteLine("    }");
        }
        else
        {
            if (contractAttribute.PacketType == PacketType.None)
            {
                writer.WriteLine($"    public {rawName}Packet()");
                writer.WriteLine("    {");
                writer.WriteLine($"        _buffer = new byte[9];");
                writer.WriteLine("    }");
                writer.WriteLine();
            }

            writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine($"    public override void Serialize(object? data = null)");
            writer.WriteLine("    {");
            writer.WriteLine("        Serialize();");
            writer.WriteLine("    }");
            writer.WriteLine();

            writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine($"    public void Serialize()");
            writer.WriteLine("    {");

            if (contractAttribute.PacketType != PacketType.None)            
                writer.WriteLine($"        ByteBuffer.WritePacketType(_buffer, 0, PacketType.{contractAttribute.PacketType.ToString()});");
            else            
                writer.WriteLine($"        ByteBuffer.WriteUShort(_buffer, 0, (ushort)ServerPacket.{contract.Name.Replace("Contract", "")});");
            
            writer.WriteLine("    }");
        }
    }

    private static void GenerateSendFunction(StreamWriter writer, Type contract, FieldInfo[] fields, ContractAttribute attribute)
    {
        var contractName = contract.Name;
        var contractRawName = contractName.Replace("Contract", "");

        var dataParam = (attribute.Flags.HasFlag(ContractPacketFlags.NoContent)) ? "" : $" , {contractName} data";
        var dataSerialize = !attribute.Flags.HasFlag(ContractPacketFlags.NoContent) ? "data" : "";

        if (fields.Length > 0)
        {
            writer.WriteLine();
            writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine($"    public void Send(Entity owner{dataParam}{(attribute.Flags.HasFlag(ContractPacketFlags.ToEntity) ? ", Entity entity" : "")})");
            writer.WriteLine("    {");
            writer.WriteLine($"        var buffer = Serialize({dataSerialize});");
        }
        else
        {
            writer.WriteLine();
            writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine($"    public void Send(Entity owner{dataParam}{(attribute.Flags.HasFlag(ContractPacketFlags.ToEntity) ? ", Entity entity" : "")})");
            writer.WriteLine("    {");
            writer.WriteLine($"        var buffer = Serialize();");
        }

        if (attribute.Flags.HasFlag(ContractPacketFlags.AreaOfInterest))
        {
            //writer.WriteLine($"        owner.Reply(ServerPacket.{contractName.Replace("DTO", "")}, buffer, {attribute.Queue.ToString().ToLower()}, {attribute.EncryptedData.ToString().ToLower()});");
        }
        else
        {
            if (attribute.Flags.HasFlag(ContractPacketFlags.Queue))
            {
                if (attribute.Flags.HasFlag(ContractPacketFlags.Self))
                {
                    writer.WriteLine($"        QueueBuffer.AddBuffer(owner.Id, buffer);");
                }
                else if (attribute.Flags.HasFlag(ContractPacketFlags.ToEntity))
                {
                    writer.WriteLine($"        QueueBuffer.AddBuffer(entity.Id, buffer);");
                }
            }
            else
            {
                if (attribute.Flags.HasFlag(ContractPacketFlags.Self))
                {
                    writer.WriteLine();
                    writer.WriteLine($"        if (EntitySocketMap.TryGet(owner.Id, out var socket))");
                    writer.WriteLine($"             socket.Send(buffer);");
                }
                else if (attribute.Flags.HasFlag(ContractPacketFlags.ToEntity))
                {
                    writer.WriteLine();
                    writer.WriteLine($"        if (EntitySocketMap.TryGet(entity.Id, out var socket))");
                    writer.WriteLine($"             socket.Send(buffer);");
                }
            }
        }

        writer.WriteLine("    }");
    }
}
