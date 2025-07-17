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

public class ContractTranspiler : AbstractTranspiler
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
            var rawName = contractName.Replace("Packet", "");

            if (attribute != null && attribute.LayerType == PacketLayerType.Server)
            {
                string filePath = Path.Combine(baseDirectoryPath, $"{contractName}.cs");

                if (attribute != null && attribute.LayerType == PacketLayerType.Server && attribute.PacketType == PacketType.None)
                {
                    serverPackets.Add(attribute.Name);
                    serverPacketType.Add(attribute.PacketType.ToString());
                    serverLowLevelPacket.Add(attribute.PacketType != PacketType.None);
                }

                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("// This file was generated automatically, please do not change it.");
                    writer.WriteLine();
                    writer.WriteLine("using System.Runtime.CompilerServices;");               
                    writer.WriteLine();
                    writer.WriteLine($"public partial struct {contractName}: INetworkPacket");                    
                    writer.WriteLine("{");

                    GenerateSerialize(writer, contract, fields, attribute);
                    //GenerateSendFunction(writer, contract, fields, attribute);

                    writer.WriteLine("}");
                }
            }
            else if (attribute != null && attribute.LayerType == PacketLayerType.Client)
            {
                string filePath = Path.Combine(baseDirectoryPath, $"{contractName}.cs");

                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("// This file was generated automatically, please do not change it.");
                    writer.WriteLine();
                    writer.WriteLine("using System.Runtime.CompilerServices;");
                    writer.WriteLine();
                    writer.WriteLine($"public partial struct {contractName}: INetworkPacketRecive");
                    writer.WriteLine("{");

                    GenerateSerialize(writer, contract, fields, attribute);

                    writer.WriteLine("}");
                }
            }

            if (attribute.LayerType == PacketLayerType.Client)
            {
                clientPackets.Add(contract.Name);
            }
        }

        GenerateEnum("ClientPackets", clientPackets, networkDirectoryPath, null, null);
        GenerateEnum("ServerPackets", serverPackets, networkDirectoryPath, serverLowLevelPacket, serverPacketType);
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
                writer.WriteLine($"    {value.Replace("Packet", "")} = {pointer},");
                pointer++;
            }

            writer.WriteLine("}");
        }
    }

    private static void GenerateSerialize(StreamWriter writer, Type contract, FieldInfo[] fields, ContractAttribute contractAttribute)
    {
        var data = contractAttribute.Flags.HasFlag(ContractPacketFlags.NoContent) ? "" : $"{contract.Name} data";
        var rawName = contract.Name.Replace("Packet", "");

        if (fields.Length > 0)
        {
            int totalBytes = (contractAttribute.PacketType != PacketType.None) ? 1 : 3;

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
                        totalBytes += 6;
                        break;
                }

                if (totalBytes == 3600)
                    break;
            }

            writer.WriteLine($"    public int Size => {totalBytes};");
            writer.WriteLine();

            //Serialize
            if(contractAttribute.LayerType == PacketLayerType.Server)
            {
                writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                writer.WriteLine($"    public void Serialize(ref FlatBuffer buffer)");
                writer.WriteLine("    {");

                if (contractAttribute.PacketType != PacketType.None)
                    writer.WriteLine($"        buffer.Write(PacketType.{contractAttribute.PacketType.ToString()});");
                else
                {
                    if (contractAttribute.Flags.HasFlag(ContractPacketFlags.Reliable))
                        writer.WriteLine($"        buffer.Write(PacketType.Reliable);");
                    else
                        writer.WriteLine($"        buffer.Write(PacketType.Unreliable);");

                    writer.WriteLine($"        buffer.Write((ushort)ServerPackets.{rawName});");
                }

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
                        case "ushort":
                        case "short":
                        case "byte":
                        case "float":
                        case "long":
                        case "decimal":
                            writer.WriteLine($"        buffer.Write({fieldName});");
                            break;
                        case "bool":
                        case "boolean":
                            writer.WriteLine($"        buffer.Write({fieldName});");
                            break;
                        case "FVector":
                        case "FRotator":
                            writer.WriteLine($"        buffer.Write({fieldName}, 0.1f);");
                            break;
                        case "id":
                            writer.WriteLine($"        buffer.Write(Base36.ToInt({fieldName}));");
                            break;
                        default:
                            writer.WriteLine($"    // Unsupported type: {fieldType}");
                            break;
                    }
                }

                writer.WriteLine("    }");
            }
            
            //Deserialize

            writer.WriteLine();
            writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine($"    public void Deserialize(ref FlatBuffer buffer)");
            writer.WriteLine("    {");

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
                        writer.WriteLine($"        {fieldName} = buffer.Read<int>();");
                        break;
                    case "uint":
                        writer.WriteLine($"        {fieldName} = buffer.Read<uint>();");
                        break;
                    case "ushort":
                        writer.WriteLine($"        {fieldName} = buffer.Read<ushort>();");
                        break;
                    case "short":
                        writer.WriteLine($"        {fieldName} = buffer.Read<short>();");
                        break;
                    case "byte":
                        writer.WriteLine($"        {fieldName} = buffer.Read<byte>();");
                        break;
                    case "float":
                        writer.WriteLine($"        {fieldName} = buffer.Read<float>();");
                        break;
                    case "long":
                        writer.WriteLine($"        {fieldName} = buffer.Read<long>();");
                        break;
                    case "ulong":
                        writer.WriteLine($"        {fieldName} = buffer.Read<ulong>();");
                        break;
                    case "bool":
                    case "boolean":
                        writer.WriteLine($"        {fieldName} = buffer.Read<bool>();");
                        break;
                    case "decimal":
                        writer.WriteLine($"        {fieldName} = (decimal)buffer.Read<float>();");
                        break;
                    case "FVector":
                        writer.WriteLine($"        {fieldName} = buffer.ReadFVector(0.1f);");
                        break;
                    case "FRotator":
                        writer.WriteLine($"        {fieldName} = buffer.ReadFRotator(0.1f);");
                        break;
                    case "id":
                        writer.WriteLine($"        {fieldName} = Base36.ToString(buffer.Read<int>());");
                        break;
                    default:
                        writer.WriteLine($"    // Unsupported type: {fieldType}");
                        break;
                }
            }


            writer.WriteLine("    }");
        }
        else
        {
            if (contractAttribute.PacketType != PacketType.None)
                writer.WriteLine($"    public int Size => 1;");
            else
                writer.WriteLine($"    public int Size => 3;");

            writer.WriteLine();

            writer.WriteLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine($"    public void Serialize(ref FlatBuffer buffer)");
            writer.WriteLine("    {");

            if (contractAttribute.PacketType != PacketType.None)
                writer.WriteLine($"        buffer.Write(PacketType.{contractAttribute.PacketType.ToString()});");
            else
            {
                if(contractAttribute.Flags.HasFlag(ContractPacketFlags.Reliable))
                    writer.WriteLine($"        buffer.Write(PacketType.Reliable);");
                else
                    writer.WriteLine($"        buffer.Write(PacketType.Unreliable);");

                writer.WriteLine($"        buffer.Write((ushort)ServerPackets.{rawName});");
            }
                            
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
