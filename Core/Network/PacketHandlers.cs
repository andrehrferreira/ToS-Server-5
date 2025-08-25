/*
* PacketHandler
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

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

public abstract class PacketHandler
{
    static readonly PacketHandler[] Handlers = new PacketHandler[1024];
    public abstract ClientPackets Type { get; }
    public abstract void Consume(PlayerController ctrl, ref FlatBuffer buffer);

        static PacketHandler()
    {
        var logMsg = "[PACKETHANDLER] 🔧 Initializing packet handlers...";
        Console.WriteLine(logMsg);
        FileLogger.Log(logMsg);

        var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
                   .SelectMany(t => t.GetTypes())
                   .Where(t => t.IsClass && t.Namespace == "Packets.Handler")
                   .ToList();

        logMsg = $"[PACKETHANDLER] 📋 Found {handlerTypes.Count} handler types in Packets.Handler namespace";
        Console.WriteLine(logMsg);
        FileLogger.Log(logMsg);

        foreach (Type t in handlerTypes)
        {
            try
            {
                if (Activator.CreateInstance(t) is PacketHandler packetHandler)
                {
                    Handlers[(int)packetHandler.Type] = packetHandler;
                    logMsg = $"[PACKETHANDLER] ✅ Registered {t.Name} for {packetHandler.Type} (index: {(int)packetHandler.Type})";
                    Console.WriteLine(logMsg);
                    FileLogger.Log(logMsg);
                }
            }
            catch (Exception ex)
            {
                logMsg = $"[PACKETHANDLER] ❌ Failed to register {t.Name}: {ex.Message}";
                Console.WriteLine(logMsg);
                FileLogger.Log(logMsg);
            }
        }

        logMsg = "[PACKETHANDLER] 🏁 Handler initialization complete";
        Console.WriteLine(logMsg);
        FileLogger.Log(logMsg);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HandlePacket(PlayerController ctrl, ref FlatBuffer buffer, ClientPackets type)
    {
        try
        {
            FileLogger.Log($"[PACKETHANDLER] 🎯 Attempting to handle {type} (index: {(int)type})");

            var handler = Handlers[(int)type];
            if (handler == null)
            {
                FileLogger.Log($"[PACKETHANDLER] ❌ No handler found for {type}");
                return;
            }

            FileLogger.Log($"[PACKETHANDLER] ✅ Handler found: {handler.GetType().Name}");
            FileLogger.Log($"[PACKETHANDLER] 📦 Buffer position: {buffer.Position}, capacity: {buffer.Capacity}");

            handler.Consume(ctrl, ref buffer);

            FileLogger.Log($"[PACKETHANDLER] ✅ Handler {handler.GetType().Name} executed successfully");
        }
        catch (Exception ex)
        {
            FileLogger.Log($"[PACKETHANDLER] ❌ Error handling {type}: {ex.Message}");
            FileLogger.Log($"[PACKETHANDLER] ❌ Stack trace: {ex.StackTrace}");
        }
    }
}

public interface IPacket
{
    FlatBuffer Buffer { get; }
    void Serialize(object? data = null);
}

public unsafe class Packet : IPacket
{
    public FlatBuffer _buffer = new FlatBuffer(1);

    static readonly Packet[] Handlers = new Packet[1024];

    public FlatBuffer Buffer
    {
        get
        {
            return _buffer;
        }
    }

    public virtual void Serialize(object? data = null)
    {
        throw new NotImplementedException("Serialization method not implemented for this packet type.");
    }

    public virtual void Send(Entity owner, object data, Entity entity)
    {
        throw new NotImplementedException("Send method not implemented for this packet type.");
    }
}

public static class PacketManager
{
    private static readonly ConcurrentDictionary<ServerPackets, IPacket> HighLevelPackets =
        new ConcurrentDictionary<ServerPackets, IPacket>();

    private static readonly ConcurrentDictionary<PacketType, IPacket> LowLevelPackets =
        new ConcurrentDictionary<PacketType, IPacket>();

    public static void Register(ServerPackets packetType, IPacket packet)
    {
        if (!HighLevelPackets.TryAdd(packetType, packet))
            throw new InvalidOperationException($"High-level packet for {packetType} already registered.");
    }

    public static void Register(PacketType packetType, IPacket packet)
    {
        if (!LowLevelPackets.TryAdd(packetType, packet))
            throw new InvalidOperationException($"Low-level packet for {packetType} already registered.");
    }

    public static bool TryGet(ServerPackets packetType, out IPacket packet)
        => HighLevelPackets.TryGetValue(packetType, out packet);

    public static bool TryGet(PacketType packetType, out IPacket packet)
        => LowLevelPackets.TryGetValue(packetType, out packet);

    public static IPacket Get(ServerPackets packetType)
        => HighLevelPackets.TryGetValue(packetType, out var packet)
            ? packet
            : throw new KeyNotFoundException($"Packet {packetType} not registered.");

    public static IPacket Get(PacketType packetType)
        => LowLevelPackets.TryGetValue(packetType, out var packet)
            ? packet
            : throw new KeyNotFoundException($"Packet {packetType} not registered.");

    public static void Clear()
    {
        HighLevelPackets.Clear();
        LowLevelPackets.Clear();
    }
}
