using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Tftp;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Tftp
{
    public class TftpClient
    {
        public TftpClient()
        {
        }

        public void Read(IPEndPoint server, string fileName, string outputPath, CancellationToken cancellationToken)
        {
            Console.WriteLine("Reading to local file '{0}' from file '{1}' on server {2}", outputPath, fileName, server);
            using (FileStream outputFileStream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (TftpConnection connection = new TftpConnection())
            {
                RemoteEndPointReservation remoteEPReservation = null;
                try
                {
                    bool initialized = false;
                    ushort currentBlockNumber = 1;
                    DataPacket currentDataPacket;
                    AckPacket ackPacket = null;
                    while (true)
                    {
                        Func<ITftpPacket, bool> receiveFilter = (ITftpPacket packet) =>
                            {
                                if (packet.Op == TftpOperation.Data)
                                {
                                    DataPacket d = (DataPacket)packet;
                                    if (d.BlockNumber == currentBlockNumber)
                                    {
                                        return true;
                                    }
                                    else Console.WriteLine("Incorrect DATA block number.");
                                }
                                else Console.WriteLine("Incorrect operation. Expected DATA.");
                                return false;
                            };
                        // To initialize the transfer, send RRQ and wait for the first data block.
                        if (!initialized)
                        {
                            Console.WriteLine("Sending RRQ");
                            // NOTE: the server only supports octet mode by design.
                            RrqPacket requestPacket = new RrqPacket(fileName, TftpMode.octet);
                            IPEndPoint remoteEP;
                            currentDataPacket = (DataPacket)connection.SendAndWaitForResponse(requestPacket, server, receiveFilter, out remoteEP, cancellationToken);
                            remoteEPReservation = new RemoteEndPointReservation(remoteEP);
                            connection.Connect(remoteEP);
                            initialized = true;
                        }
                        else
                        {
                            Console.WriteLine("Sending ack for block number {0}", ackPacket.BlockNumber);
                            currentDataPacket = (DataPacket)connection.SendAndWaitForResponse(ackPacket, receiveFilter, cancellationToken);
                        }
                        Console.WriteLine("DATA packet containing block {0} received", currentDataPacket.BlockNumber);
                        outputFileStream.Write(currentDataPacket.Data, 0, currentDataPacket.Data.Length);
                        ackPacket = new AckPacket(currentBlockNumber);
                        // When the received DATA packet is less than the maximum length, send a final ack and terminate.
                        if (currentDataPacket.Data.Length < DataPacket.MaxBlockSize)
                        {
                            connection.Send(ackPacket);
                            break;
                        }
                        ++currentBlockNumber;
                    }
                }
                finally
                {
                    if (remoteEPReservation != null)
                        remoteEPReservation.Dispose();
                }
            }
        }

        public void Write(IPEndPoint server, string fileName, string inputPath, CancellationToken cancellationToken)
        {
            Console.WriteLine("Writing local file '{0}' to file '{1}' on server {2}", inputPath, fileName, server);

            // Read the file from the local file system.
            byte[] file = File.ReadAllBytes(inputPath);
            
            // Create a connection with the server.
            IPEndPoint receivedFromEP = new IPEndPoint(IPAddress.Any, 0);
            using (RemoteEndPointReservation clientReservation = new RemoteEndPointReservation(server))
            using (TftpConnection connection = new TftpConnection())
            {
                RemoteEndPointReservation remoteEPReservation = null;
                try
                {
                    // Read the file contents to the remote endpoint in chunks.
                    ushort currentBlockNumber = 0;
                    int currentFileIndex = 0;
                    bool done = false;
                    while (!done)
                    {
                        Func<ITftpPacket, bool> receiveFilter = (ITftpPacket packet) =>
                        {
                            if (packet.Op == TftpOperation.Ack)
                            {
                                AckPacket ack = (AckPacket)packet;
                                if (ack.BlockNumber == currentBlockNumber)
                                {
                                    Console.WriteLine("ACK received for block {0}", ack.BlockNumber);
                                    return true;
                                }
                                else Console.WriteLine("Incorrect ACK block number {0}", ack.BlockNumber);
                            }
                            else Console.WriteLine("Incorrect operation. Expected ACK.");
                            return false;
                        };
                        ITftpPacket sendPacket;
                        // To initialize the transfer, send WRQ and wait for an ACK of block 0.
                        if (currentBlockNumber == 0)
                        {
                            Console.WriteLine("Sending WRQ");
                            // NOTE: the server only supports octet mode by design.
                            sendPacket = new WrqPacket(fileName, TftpMode.octet);
                            IPEndPoint remoteEP;
                            connection.SendAndWaitForResponse(sendPacket, server, receiveFilter, out remoteEP, cancellationToken);
                            remoteEPReservation = new RemoteEndPointReservation(remoteEP);
                            connection.Connect(remoteEP);
                        }
                        else
                        {
                            // Extract the current block from the file contents and put it in a DATA packet.
                            Console.WriteLine("Sending block {0}", currentBlockNumber);
                            int remainingBytes = file.Length - currentFileIndex;
                            int blockSize;
                            if (remainingBytes < DataPacket.MaxBlockSize)
                            {
                                blockSize = remainingBytes;
                                done = true;
                            }
                            else
                            {
                                blockSize = DataPacket.MaxBlockSize;
                            }
                            byte[] block = new byte[blockSize];
                            Buffer.BlockCopy(file, currentFileIndex, block, 0, blockSize);
                            currentFileIndex += block.Length;
                            sendPacket = new DataPacket(blockNumber: currentBlockNumber, data: block);

                            // Send the DATA packet and wait for the corresponding ACK.
                            connection.SendAndWaitForResponse(sendPacket, receiveFilter, cancellationToken);
                        }

                        // Move to the next block.
                        currentBlockNumber++;
                    }
                }
                finally
                {
                    if (remoteEPReservation != null)
                        remoteEPReservation.Dispose();
                }
            }
        }
    }
}
