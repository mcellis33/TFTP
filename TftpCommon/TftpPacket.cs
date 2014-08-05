using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Tftp
{
    public enum TftpOperation : ushort
    {
        ReadRequest = 1,
        WriteRequest = 2,
        Data = 3,
        Ack = 4,
        Error = 5
    }

    public enum TftpMode
    {
        unknown,
        netascii,
        octet,
        mail,
    }

    public enum TftpError
    {
        Undefined = 0,
        FileNotFound = 1,
        AccessViolation = 2,
        DiskFullOrAllocationExceeded = 3,
        IllegalTftpOperation = 4,
        UnknownTransferId = 5,
        FileAlreadyExists = 6,
        NoSuchUser = 7
    }

    public interface ITftpPacket
    {
        TftpOperation Op { get; }
    }

    public interface IRequestPacket : ITftpPacket
    {
        string FileName { get; }
        TftpMode Mode { get; }
    }

    public class RrqPacket : IRequestPacket
    {
        public TftpOperation Op { get { return TftpOperation.ReadRequest; } }
        public string FileName { get; private set; }
        public TftpMode Mode { get; private set; }

        public RrqPacket(string fileName, TftpMode mode)
        {
            FileName = fileName;
            Mode = mode;
        }
    }

    public class WrqPacket : IRequestPacket
    {
        public TftpOperation Op { get { return TftpOperation.WriteRequest; } }
        public string FileName { get; private set; }
        public TftpMode Mode { get; private set; }
        
        public WrqPacket(string fileName, TftpMode mode)
        {
            FileName = fileName;
            Mode = mode;
        }
    }

    public class DataPacket : ITftpPacket
    {
        public TftpOperation Op { get { return TftpOperation.Data; } }
        public ushort BlockNumber { get; private set; }
        public byte[] Data { get; private set; }

        // Maximum Length of Data.
        public const int MaxBlockSize = 512;

        public DataPacket(ushort blockNumber, byte[] data)
        {
            BlockNumber = blockNumber;
            Data = data;
        }
    }

    public class AckPacket : ITftpPacket
    {
        public TftpOperation Op { get { return TftpOperation.Ack; } }
        public ushort BlockNumber { get; private set; }

        public AckPacket(ushort blockNumber)
        {
            BlockNumber = blockNumber;
        }
    }

    public class ErrorPacket : ITftpPacket
    {
        public TftpOperation Op { get { return TftpOperation.Error; } }
        public TftpError Error { get; private set; }
        public string Message { get; private set; }

        public ErrorPacket(TftpError error, string message)
        {
            Error = error;
            Message = message;
        }
    }
}
