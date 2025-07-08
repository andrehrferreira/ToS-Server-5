// This file was generated automatically, please do not change it.

using System.Runtime.CompilerServices;

public class UpdateEntityPacket: Packet
{
    public UpdateEntityPacket()
    {
        _buffer = new byte[35];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Serialize(object? data = null)
    {
        var typedData = data is UpdateEntity p ? p : throw new InvalidCastException("Invalid data type.");
        Serialize(typedData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(UpdateEntity data)
    {
        uint offset = 0;
        offset = ByteBuffer.WriteUShort(_buffer, offset, (ushort)ServerPacket.UpdateEntity);
        offset = ByteBuffer.WriteUInt(_buffer, offset, data.EntityId);
        offset = ByteBuffer.WriteFVector(_buffer, offset, data.Positon);
        offset = ByteBuffer.WriteFRotator(_buffer, offset, data.Rotator);
        offset = ByteBuffer.WriteUShort(_buffer, offset, data.AnimationState);
        offset = ByteBuffer.WriteUInt(_buffer, offset, data.Flags);
    }
}
