using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tftp;
using System.Diagnostics;
using System.Security.Cryptography;

namespace TftpUnitTest
{
    /* TODO:
     * - Capture packets from 3rd party TFTP client/server traffic and use them to validate parsing and serialization.
     */

    [TestClass]
    public class TftpPacketUtilsTests
    {
        [TestMethod]
        public void ParseNetascii_SingleString()
        {
            byte[] b = new byte[] { 65, 66, 0 };
            int pos = 0;
            string s = TftpPacketUtils.ParseNetascii(b, ref pos);
            Debug.Assert(pos == 3);
            Debug.Assert(s == "AB");
        }

        [TestMethod]
        public void ParseNetascii_MultipleStrings()
        {
            byte[] b = new byte[] { 65, 66, 0, 67, 68, 0, 69, 70, 0};
            int pos = 0;
            string s1 = TftpPacketUtils.ParseNetascii(b, ref pos);
            Debug.Assert(pos == 3);
            Debug.Assert(s1 == "AB");
            string s2 = TftpPacketUtils.ParseNetascii(b, ref pos);
            Debug.Assert(pos == 6);
            Debug.Assert(s2 == "CD");
            string s3 = TftpPacketUtils.ParseNetascii(b, ref pos);
            Debug.Assert(pos == 9);
            Debug.Assert(s3 == "EF");
        }

        [TestMethod]
        public void ParseNetascii_OnlyNullTerminator_ReturnsEmptyString()
        {
            byte[] b = new byte[] { 0 };
            int pos = 0;
            string s = TftpPacketUtils.ParseNetascii(b, ref pos);
            Debug.Assert(pos == 1);
            Debug.Assert(s == "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ParseNetascii_NoNullTerminator_ThrowsArgumentException()
        {
            byte[] b = new byte[] { 65, 66 };
            int pos = 0;
            TftpPacketUtils.ParseNetascii(b, ref pos);
        }

        [TestMethod]
        public void Serialize_RrqPacket()
        {
            RrqPacket rrq = new RrqPacket("asdf/zxcv", TftpMode.octet);
            byte[] bytes = TftpPacketUtils.Serialize(rrq);
            rrq = (RrqPacket)TftpPacketUtils.Parse(bytes);
            Debug.Assert(rrq.FileName == "asdf/zxcv");
            Debug.Assert(rrq.Mode == TftpMode.octet);
        }

        [TestMethod]
        public void Serialize_WrqPacket()
        {
            WrqPacket wrq = new WrqPacket("qwer/asdf", TftpMode.netascii);
            byte[] bytes = TftpPacketUtils.Serialize(wrq);
            wrq = (WrqPacket)TftpPacketUtils.Parse(bytes);
            Debug.Assert(wrq.FileName == "qwer/asdf");
            Debug.Assert(wrq.Mode == TftpMode.netascii);
        }

        [TestMethod]
        public void Serialize_DataPacket()
        {
            byte[] payload = new byte[512];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(payload);
            DataPacket data = new DataPacket(450, payload);
            byte[] bytes = TftpPacketUtils.Serialize(data);
            data = (DataPacket)TftpPacketUtils.Parse(bytes);
            Debug.Assert(data.BlockNumber == 450);
            for (int i = 0; i < 512; ++i)
                Debug.Assert(data.Data[i] == payload[i]);
        }

        [TestMethod]
        public void Serialize_AckPacket()
        {
            AckPacket ack = new AckPacket(333);
            byte[] bytes = TftpPacketUtils.Serialize(ack);
            ack = (AckPacket)TftpPacketUtils.Parse(bytes);
            Debug.Assert(ack.BlockNumber == 333);
        }

        [TestMethod]
        public void Serialize_ErrorPacket()
        {
            ErrorPacket error = new ErrorPacket(error: TftpError.IllegalTftpOperation, message: "Oh noes!");
            byte[] bytes = TftpPacketUtils.Serialize(error);
            error = (ErrorPacket)TftpPacketUtils.Parse(bytes);
            Debug.Assert(error.Error == TftpError.IllegalTftpOperation);
            Debug.Assert(error.Message == "Oh noes!");
        }
    }
}
