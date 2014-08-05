using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TftpUnitTest
{
    [TestClass]
    public class Server
    {
        [TestMethod]
        public void WriteThenRead()
        {
             WindowsTftpClient.Run()
        }
    }
}
