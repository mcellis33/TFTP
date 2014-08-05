using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Tftp
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(500); // Sleep to demonstrate client timeout/retry behavior.
            var server = new TftpServer();
            Console.ReadKey();
            server.Stop();
        }
    }
}
