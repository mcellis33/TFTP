using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Tftp
{
    public static class TftpPacketUtils
    {
        public static bool TryParse(byte[] bytes, out ITftpPacket packet)
        {
            try
            {
                packet = Parse(bytes);
                return true;
            }
            catch
            {
                packet = null;
                return false;
            }
        }

        // NOTE: TFTP packets use network byte order (big-endian). This does not appear to be mentioned in the RFC.
        public static ITftpPacket Parse(byte[] bytes)
        {
            int index = 0;
            TftpOperation op = (TftpOperation)EndianBitConverter.Big.ToUInt16(bytes, index);
            index += 2;

            string fileName;
            TftpMode mode;
            ushort blockNumber;
            switch (op)
            {
                case TftpOperation.ReadRequest:
                    fileName = ParseNetascii(bytes, ref index);
                    mode = ParseMode(bytes, ref index);
                    return new RrqPacket(fileName, mode);

                case TftpOperation.WriteRequest:
                    fileName = ParseNetascii(bytes, ref index);
                    mode = ParseMode(bytes, ref index);
                    return new WrqPacket(fileName, mode);

                case TftpOperation.Data:
                    blockNumber = EndianBitConverter.Big.ToUInt16(bytes, index);
                    index += 2;
                    byte[] data = new byte[bytes.Length - index];
                    Buffer.BlockCopy(bytes, index, data, 0, data.Length);
                    return new DataPacket(blockNumber, data);

                case TftpOperation.Ack:
                    blockNumber = EndianBitConverter.Big.ToUInt16(bytes, index);
                    return new AckPacket(blockNumber);

                case TftpOperation.Error:
                    TftpError error = (TftpError)EndianBitConverter.Big.ToUInt16(bytes, index);
                    index += 2;
                    string message = ParseNetascii(bytes, ref index);
                    return new ErrorPacket(error, message);

                default:
                    throw new Exception("Operation not recognized.");
            }
        }

        public static string ParseNetascii(byte[] data, ref int index)
        {
            if (index >= data.Length)
                throw new IndexOutOfRangeException("index must be within data");
            int startIndex = index;
            while (data[index] != 0)
            {
                ++index;
                if (index >= data.Length)
                    throw new ArgumentException("Null terminator not found.");
            }
            int charCount = index - startIndex;
            ++index; // Move past null terminator.
            return System.Text.Encoding.ASCII.GetString(data, startIndex, charCount);
        }

        private static TftpMode ParseMode(byte[] data, ref int pos)
        {
            string s = ParseNetascii(data, ref pos);

            if (string.Equals(s, "netascii", StringComparison.OrdinalIgnoreCase))
                return TftpMode.netascii;

            else if (string.Equals(s, "octet", StringComparison.OrdinalIgnoreCase))
                return TftpMode.octet;

            // octet mode was formerly known as binary
            else if (string.Equals(s, "binary", StringComparison.OrdinalIgnoreCase))
                return TftpMode.octet;

            else if (string.Equals(s, "mail"))
                return TftpMode.mail;

            else
                return TftpMode.unknown;
        }

        public static byte[] Serialize(this ITftpPacket packet)
        {
            MemoryStream bytes = new MemoryStream();
            bytes.Write(EndianBitConverter.Big.GetBytes((ushort)packet.Op), 0, sizeof(ushort));
            switch (packet.Op)
            {
                case TftpOperation.ReadRequest:
                    RrqPacket rrq = (RrqPacket)packet;
                    StringToNetascii(rrq.FileName, bytes);
                    StringToNetascii(rrq.Mode.ToString(), bytes);
                    break;

                case TftpOperation.WriteRequest:
                    WrqPacket wrq = (WrqPacket)packet;
                    StringToNetascii(wrq.FileName, bytes);
                    StringToNetascii(wrq.Mode.ToString(), bytes);
                    break;

                case TftpOperation.Data:
                    DataPacket data = (DataPacket)packet;
                    bytes.Write(EndianBitConverter.Big.GetBytes((ushort)data.BlockNumber), 0, sizeof(ushort));
                    bytes.Write(data.Data, 0, data.Data.Length);
                    break;

                case TftpOperation.Ack:
                    AckPacket ack = (AckPacket)packet;
                    bytes.Write(EndianBitConverter.Big.GetBytes((ushort)ack.BlockNumber), 0, sizeof(ushort));
                    break;

                case TftpOperation.Error:
                    ErrorPacket error = (ErrorPacket)packet;
                    bytes.Write(EndianBitConverter.Big.GetBytes((ushort)error.Error), 0, sizeof(ushort));
                    StringToNetascii(error.Message, bytes);
                    break;

                default:
                    throw new Exception("Operation not recognized.");
            }
            return bytes.ToArray();
        }

        public static void StringToNetascii(string s, MemoryStream bytes)
        {
            bytes.Write(System.Text.Encoding.ASCII.GetBytes(s.ToCharArray()), 0, s.Length);
            bytes.WriteByte(0); // Null terminator
        }
    }
}