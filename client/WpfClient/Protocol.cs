using System;

namespace WpfClient
{
    public enum PacketType : byte
    {
        Handshake = 0x00,
        Draw = 0x01,
        Chat = 0x02,
        Clear = 0x03
    }

    public class Protocol
    {
        public static byte[] CreatePacket(PacketType type, byte[] payload)
        {
            // Length (4) + Type (1) + Payload (N)
            int length = payload.Length + 1;
            byte[] packet = new byte[4 + length];

            // 1. Length (Little Endian)
            byte[] lenBytes = BitConverter.GetBytes(length);
            Array.Copy(lenBytes, 0, packet, 0, 4);

            // 2. Type
            packet[4] = (byte)type;

            // 3. Payload
            Array.Copy(payload, 0, packet, 5, payload.Length);

            return packet;
        }
    }
}
