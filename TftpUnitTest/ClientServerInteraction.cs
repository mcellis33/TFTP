using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tftp;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace TftpUnitTest
{
    [TestClass]
    public class ClientServerInteraction
    {
        [TestMethod]
        public void WriteThenRead()
        {
            int fileSize = 8192;
            using (TestFile inputFile = new TestFile(fileSize))
            using (TestFile outputFile = new TestFile())
            {
                TftpClient tftpClient = new TftpClient();
                TftpServer tftpServer = new TftpServer();
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 69);
                string remoteName = "file0";
                CancellationTokenSource cts = new CancellationTokenSource();
                tftpClient.Write(remoteEP, remoteName, inputFile.Path, cts.Token);
                tftpClient.Read(remoteEP, remoteName, outputFile.Path, cts.Token);
                byte[] inputBytes = File.ReadAllBytes(inputFile.Path);
                byte[] outputBytes = File.ReadAllBytes(outputFile.Path);
                Debug.Assert(inputBytes.Length == outputBytes.Length);
                for(int i = 0; i < inputBytes.Length; ++i)
                {
                    Debug.Assert(inputBytes[i] == outputBytes[i]);
                }
                tftpServer.Stop();
            }
        }
    }
}
