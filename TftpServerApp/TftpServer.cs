using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Tftp
{
    public class TftpServer
    {
        public static int DefaultBindPort { get { return 69; } }

        private Task _listenerTask;
        private ICollection<Task> _requestHandlerTasks;
        private CancellationTokenSource _cancellationTokenSource;
        private Dictionary<string, byte[]> _files;

        public TftpServer() : this(DefaultBindPort)
        {
        }

        public TftpServer(int bindPort)
        {
            _files = new Dictionary<string, byte[]>();
            _cancellationTokenSource = new CancellationTokenSource();
            _requestHandlerTasks = new List<Task>();
            _listenerTask = Task.Factory.StartNew(
                action: () => Listen(bindPort, _cancellationTokenSource.Token),
                cancellationToken: _cancellationTokenSource.Token,
                creationOptions: TaskCreationOptions.LongRunning,
                scheduler: TaskScheduler.Default);
        }

        // Listen for TFTP request packets on the port specified by bindPort.
        private void Listen(int bindPort, CancellationToken cancellationToken)
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            using (TftpConnection listener = new TftpConnection(bindPort))
            {
                Func<ITftpPacket, bool> requestFilter = (ITftpPacket packet) =>
                {
                    IRequestPacket rq = packet as IRequestPacket;
                    if (rq != null)
                    {
                        if (rq.Mode == TftpMode.octet)
                        {
                            return true;
                        }
                        else Console.WriteLine("Modes other than octet are not supported.");
                    }
                    else Console.WriteLine("Main listener recieved non-request TFTP packet.");
                    return false;
                };
                while (true)
                {
                    IRequestPacket request = (IRequestPacket)listener.Receive(requestFilter, out remoteEP, cancellationToken);
                    _requestHandlerTasks.Add(Task.Factory.StartNew(
                        action: () => ExecuteRequest(request, remoteEP, _cancellationTokenSource.Token),
                        cancellationToken: _cancellationTokenSource.Token,
                        creationOptions: TaskCreationOptions.LongRunning,
                        scheduler: TaskScheduler.Default));
                }
            }
        }

        private void ExecuteRequest(IRequestPacket request, IPEndPoint clientEP, CancellationToken cancellationToken)
        {
            switch(request.Op)
            {
                case TftpOperation.ReadRequest:
                    ExecuteRead(request.FileName, clientEP, cancellationToken);
                    break;
                case TftpOperation.WriteRequest:
                    ExecuteWrite(request.FileName, clientEP, cancellationToken);
                    break;
                default:
                    Console.WriteLine("Requested operation unknown");
                    break;
            }
        }

        // Send the file named fileName to remoteEP.
        private void ExecuteRead(string fileName, IPEndPoint clientEP, CancellationToken cancellationToken)
        {
            Console.WriteLine("RRQ for file '{0}' received from client '{1}'", fileName, clientEP);

            // Reserve a local port and create a connection with the client.
            IPEndPoint receivedFromEP = new IPEndPoint(IPAddress.Any, 0);
            using (RemoteEndPointReservation clientReservation = new RemoteEndPointReservation(clientEP))
            using (TftpConnection connection = new TftpConnection())
            {
                connection.Connect(clientEP);

                // Check if the file exists. If so, get the file contents.
                bool fileFound;
                byte[] file = null;
                lock(_files)
                {
                    fileFound = _files.ContainsKey(fileName);
                    if (fileFound)
                    {
                        // WARNING: files are read-only and do not support deletion in the protocol spec as of 6/9/14, so we do not need to make a copy of the file data.
                        file = _files[fileName];
                    }
                }

                // If the file was not found, send an error to the client and terminate the connection.
                if (!fileFound)
                {
                    string message = string.Format("Could not find file '{0}'", fileName);
                    Console.WriteLine(message);
                    connection.Send(new ErrorPacket(
                        error: TftpError.FileNotFound,
                        message: message));
                    return;
                }

                // Read the file contents to the remote endpoint in chunks.
                ushort currentBlockNumber = 1;
                int currentFileIndex = 0;
                bool done = false;
                while (!done)
                {
                    // Extract the current block from the file contents and put it in a DATA packet.
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
                    ITftpPacket sendPacket = new DataPacket(blockNumber: currentBlockNumber, data: block);

                    // Send the DATA packet and wait for the corresponding ACK.
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
                    Console.WriteLine("Sending block {0}", currentBlockNumber);
                    connection.SendAndWaitForResponse(
                        sendPacket,
                        receiveFilter,
                        cancellationToken);

                    // Move to the next block.
                    currentBlockNumber++;
                    currentFileIndex += block.Length;
                }
            }
        }

        // Write the file named fileName using data from remoteEP.
        private void ExecuteWrite(string fileName, IPEndPoint clientEP, CancellationToken cancellationToken)
        {
            Console.WriteLine("WRQ for file '{0}' received from client {1}", fileName, clientEP);

            // Reserve a local port and create a connection with the client.
            using (MemoryStream fileInMemory = new MemoryStream())
            using (RemoteEndPointReservation clientReservation = new RemoteEndPointReservation(clientEP))
            using (TftpConnection connection = new TftpConnection())
            {
                connection.Connect(clientEP);

                ushort currentBlockNumber = 1;
                DataPacket currentDataPacket;
                AckPacket ackPacket = new AckPacket(0);
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
                    Console.WriteLine("Sending ack for block number {0}", ackPacket.BlockNumber);
                    currentDataPacket = (DataPacket)connection.SendAndWaitForResponse(ackPacket, receiveFilter, cancellationToken);
                    Console.WriteLine("DATA packet containing block {0} received", currentDataPacket.BlockNumber);
                    fileInMemory.Write(currentDataPacket.Data, 0, currentDataPacket.Data.Length);
                    ackPacket = new AckPacket(currentBlockNumber);

                    // When the received DATA packet is less than the maximum length, that is the final DATA packet.
                    if (currentDataPacket.Data.Length < DataPacket.MaxBlockSize)
                    {
                        lock (_files)
                        {
                            // If the file already exists, send an error.
                            if (_files.ContainsKey(fileName))
                            {
                                string message = string.Format("File {0} already exists.", fileName);
                                Console.WriteLine(message);
                                connection.Send(new ErrorPacket(
                                    error: TftpError.FileAlreadyExists,
                                    message: message));
                            }
                            // Otherwise, commit the received bytes to the file dictionary and send the final ACK.
                            else
                            {
                                _files[fileName] = fileInMemory.ToArray();
                                connection.Send(ackPacket);
                            }
                        }
                        break;
                    }
                    ++currentBlockNumber;
                }
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                _listenerTask.Wait();
            }
            catch (AggregateException ex)
            {
                ex.Handle(e => e is TaskCanceledException);
            }
            try
            {
                Task.WaitAll(_requestHandlerTasks.ToArray());
            }
            catch (AggregateException ex)
            {
                ex.Handle(e => e is TaskCanceledException);
            }
        }
    }
}