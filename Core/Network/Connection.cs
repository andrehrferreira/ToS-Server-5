/*
* Connection
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

using NanoSockets;

namespace Wormhole
{
    public sealed class Connection : IEquatable<Connection>
    {
        public uint Id;

        public Address RemoteAddress;

        public ConnectionState State;

        public SecureSession Session;

        public long ConnectingTime;

        public uint Ping = 0;

        public DateTime PingSentAt;

        public float TimeoutLeft = 30f;

        public uint Sequence = 0;

        public uint NextSequence
        {
            get
            {
                unchecked { Sequence++; }

                return Sequence;
            }
        }

        public bool ExpiredConnecting
        {
            get
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return (State == ConnectionState.Connecting && (now - ConnectingTime) > 0);
            }
        }

        public bool Equals(Connection? other)
        {
            if(other is null) return false;

            if(ReferenceEquals(this, other)) return true;

            return RemoteAddress.Equals(other.RemoteAddress);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Connection);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RemoteAddress, Id);
        }

#pragma warning disable CS1591 
        public static bool operator ==(Connection left, Connection right)
#pragma warning restore CS1591
        {
            if (left is null)
            {
                if (right is null)
                    return true;

                return false;
            }

            return left.Equals(right);
        }

#pragma warning disable CS1591 
        public static bool operator !=(Connection left, Connection right) => !(left == right);
#pragma warning restore CS1591

        public bool ValidateToken(byte[] token, Address address)
        {
            return AddressValidationToken.Validate(token, address);
        }

        public void ParseSecurePacket(PacketFlags flags, ref ByteBuffer payload)
        {
            //if ((flags & PacketFlags.Encrypted) != 0)
            //    throw new NotImplementedException("Descript payload not implemented");

            //if ((flags & PacketFlags.Compressed) != 0)
            //    throw new NotImplementedException("Compress payload not implemented");
        }

        public void Disconnect(DisconnectReason reason, bool localDisconnect = false)
        {
            if (State == ConnectionState.Diconnected)
                return;

            State = ConnectionState.Diconnected;

            if(localDisconnect)
                Send(PacketType.Disconnect);
        }

        public void Send(PacketType type, PacketFlags flags = PacketFlags.None)
        {
            if(State == ConnectionState.Diconnected)
                return;

            Server.Send(type, this, flags);
        }

        public void Send(PacketType type, ByteBuffer buffer, PacketFlags flags = PacketFlags.None)
        {
            if (State == ConnectionState.Diconnected)
                return;

            Server.Send(type, ref buffer, this, flags);
        }
    }
}
