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

using System.Diagnostics.Contracts;
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

                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("// This file was generated automatically, please do not change it.");
                    writer.WriteLine();
                    writer.WriteLine("using System.Runtime.CompilerServices;");
                    writer.WriteLine();
                    writer.WriteLine($"public class {rawName}Packet: Packet");
                    writer.WriteLine("{");

                    GenerateSerialize(writer, contract, fields, attribute);
                    GenerateSendFunction(writer, contract, fields, attribute);

                    writer.WriteLine("}");
                }
            }

            if (attribute.LayerType == PacketLayerType.Client)
            {
                clientPackets.Add(contract.Name);
            }
        }

        GenerateEnum("ClientPacket", clientPackets, networkDirectoryPath);
        GenerateEnum("ServerPacket", serverPackets, networkDirectoryPath);
    }

    private static void GenerateEnum(string enumName, List<string> values, string directoryPath)
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

            if(enumName == "ServerPacket")
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

        if(fields.Length > 0)
        {
            writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine($"    public override ByteBuffer Serialize(object? data = null)");
            writer.WriteLine("    {");
            writer.WriteLine($"        var typedData = data is {contract.Name} p ? p : throw new InvalidCastException(\"Invalid data type.\");");
            writer.WriteLine("        return Serialize(typedData);");
            writer.WriteLine("    }");
            writer.WriteLine();

            writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine($"    public ByteBuffer Serialize({data})");
            writer.WriteLine("    {");
            writer.WriteLine("        var buffer = ByteBuffer.CreateEmptyBuffer();");

            if (contractAttribute.Flags.HasFlag(ContractPacketFlags.Reliable))
                writer.WriteLine("        buffer.Reliable = true;");

            writer.WriteLine($"        buffer.Write((byte)ServerPacket.{contract.Name.Replace("Contract", "")});");

            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<ContractFieldAttribute>();
                var fieldType = attribute.Type;
                var fieldName = field.Name;

                switch (fieldType)
                {
                    case "integer":
                    case "int":
                    case "uint":
                    case "ushort":
                    case "short":
                    case "int32":
                    case "int16":
                    case "str":
                    case "string":
                    case "byte":
                    case "float":
                    case "long":
                    case "bool":
                    case "boolean":
                    case "FVector":
                    case "FRotator":
                        writer.WriteLine($"        buffer.Write(data.{fieldName});");
                        break;
                    case "id":
                        writer.WriteLine($"        buffer.Write(Base36.ToInt(data.{fieldName}));");
                        break;
                    case "decimal":
                        writer.WriteLine($"        buffer.Write((float)data.{fieldName});");
                        break;
                    default:
                        writer.WriteLine($"    // Tipo não suportado: {fieldType}");
                        break;
                }

            }

            writer.WriteLine("        return buffer;");
            writer.WriteLine("    }");
        }
        else
        {
            writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine($"    public override ByteBuffer Serialize(object? data = null)");
            writer.WriteLine("    {");
            writer.WriteLine("        return Serialize();");
            writer.WriteLine("    }");
            writer.WriteLine();

            writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine($"    public ByteBuffer Serialize()");
            writer.WriteLine("    {");
            writer.WriteLine("        var buffer = ByteBuffer.CreateEmptyBuffer();");

            if (contractAttribute.Flags.HasFlag(ContractPacketFlags.Reliable))
                writer.WriteLine("        buffer.Reliable = true;");

            writer.WriteLine($"        buffer.Write((byte)ServerPacket.{contract.Name.Replace("Contract", "")});");
            writer.WriteLine("        return buffer;");
            writer.WriteLine("    }");
        }

        
    }

    private static void GenerateSendFunction(StreamWriter writer, Type contract, FieldInfo[] fields, ContractAttribute attribute)
    {
        var contractName = contract.Name;
        var contractRawName = contractName.Replace("Contract", "");

        var dataParam = (attribute.Flags.HasFlag(ContractPacketFlags.NoContent)) ? "" : $" , {contractName} data";
        var dataSerialize = !attribute.Flags.HasFlag(ContractPacketFlags.NoContent) ? "data" : "";

        if(fields.Length > 0)
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
