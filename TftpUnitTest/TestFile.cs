using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace TftpUnitTest
{
    class TestFile : IDisposable
    {
        private static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

        public string Path { get; private set; }

        public int Size { get; private set; }

        // Generate a file name only. Size will be 0.
        public TestFile()
        {
            Path = System.IO.Path.GetFullPath(Guid.NewGuid().ToString());
            Size = 0;
        }

        // Generate a file of the given size in bytes.
        public TestFile(int size)
        {
            Path = System.IO.Path.GetFullPath(Guid.NewGuid().ToString());
            Size = size;
            byte[] fileBytes = new byte[Size];
            rng.GetBytes(fileBytes);
            File.WriteAllBytes(Path, fileBytes);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                try
                {
                    File.Delete(Path);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to delete test file {0} due to error: {1}", Path, e);
                }
            }
        }
    }
}
