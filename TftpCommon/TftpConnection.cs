using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Tftp;

namespace Tftp
{
    public class TftpConnection : IDisposable
    {
        public TimeSpan DefaultRetryPeriod { get { return TimeSpan.FromMilliseconds(200); } }

        // Timeout for all acknowledgements.
        public TimeSpan DefaultResponseTimeout { get { return TimeSpan.FromSeconds(10); } }

        private UdpClient _udp;

        private object _defaultRemoteEPLock;
        private IPEndPoint _defaultRemoteEP;

        private object _receiveFilterLock;
        private Func<ITftpPacket, bool> _receiveFilter;

        private BlockingCollection<ReceiveResult> _receivedPackets;

        private bool _disposed;

        public TftpConnection() : this(0)
        {
        }

        // bindPort - the local port to which this connection should bind.
        public TftpConnection(int bindPort)
        {
            _udp = new UdpClient(bindPort);
            Console.WriteLine("Opened TftpConnection on port {0}", ((IPEndPoint)_udp.Client.LocalEndPoint).Port);
            _defaultRemoteEPLock = new Object();
            _defaultRemoteEP = null;
            _receiveFilterLock = new Object();
            _receiveFilter = null;
            // WARNING: For now, we only need to receive one packet at a time.
            _receivedPackets = new BlockingCollection<ReceiveResult>(1);
            _udp.BeginReceive((AsyncCallback)ReceiveCallback, null);
        }

        /* 
         * This callback implements the main receive loop for UDP packets on this TftpConnection.
         * It is effectively a filtered producer thread of ITftpPackets.
         * All undesired datagrams are filtered out. Undesired means:
         * - non-TFTP datagrams
         * - datagrams from an incorrect source (ones that are not sent from the default remote endpoint if it is set)
         * - TFTP packets for which receiveFilter returns false
         * If a datagram is not filtered out, the resulting ITftpPacket is stored in _receivedPackets.
         * This callback is only to be passed to _udp.BeginReceive. It 'recurses' by calling BeginReceive again.
         */
        private void ReceiveCallback(IAsyncResult ar)
        {
            IPEndPoint receivedFromEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] receivedBytes = null;
            try
            {
                receivedBytes = _udp.EndReceive(ar, ref receivedFromEP);
            }
            // TODO: _udp has been closed.
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException e)
            {
                Console.WriteLine("Caught SocketException from EndReceive: {0}", e);
            }

            if (receivedBytes != null)
            {
                // Try to parse the received datagram as a TFTP packet.
                ITftpPacket receivedPacket = null;
                if (TftpPacketUtils.TryParse(receivedBytes, out receivedPacket))
                {
                    // Check whether the packet is the one we are looking for.
                    bool isResponseValid = false;
                    lock (_receiveFilterLock)
                    {
                        if (_receiveFilter != null)
                            isResponseValid = _receiveFilter(receivedPacket);
                    }
                    if (isResponseValid)
                    {
                        // If the default remote host is set, and the packet is not from that host, then ignore it and send an error back.
                        bool isRemoteEPCorrect = true;
                        lock (_defaultRemoteEPLock)
                        {
                            if (_defaultRemoteEP != null)
                                isRemoteEPCorrect = receivedFromEP.Equals(_defaultRemoteEP);
                        }
                        if (isRemoteEPCorrect)
                        {
                            if (!_receivedPackets.TryAdd(new ReceiveResult(receivedPacket, receivedFromEP)))
                                Console.WriteLine("Dropping valid packet since one has already been received.");
                        }
                        else
                        {
                            Console.WriteLine("Message received from incorrect endpoint.");
                            Send(new ErrorPacket(
                                error: TftpError.UnknownTransferId, // TODO: is this the correct error to send?
                                message: "Incorrect endpoint"));
                        }
                    }
                    else Console.WriteLine("TFTP packet not valid.");
                }
                else Console.WriteLine("Failed to parse packet.");
            }
            else Console.WriteLine("EndReceive failed.");

            // This method chains into itself until _udp is closed.
            try
            {
                _udp.BeginReceive(ReceiveCallback, null);
            }
            // TODO: _udp has been closed.
            catch (ObjectDisposedException)
            {
                return;
            }
        }

        // Send to the default remote host
        public ITftpPacket SendAndWaitForResponse(
            ITftpPacket sendPacket,
            Func<ITftpPacket, bool> receiveFilter,
            CancellationToken cancellationToken)
        {
            if (_defaultRemoteEP == null)
                throw new SocketException((int)SocketError.DestinationAddressRequired);
            IPEndPoint receivedFromEP = new IPEndPoint(IPAddress.Any, 0);
            return SendAndWaitForResponse(
                sendPacket,
                _defaultRemoteEP,
                DefaultResponseTimeout,
                DefaultRetryPeriod,
                receiveFilter,
                out receivedFromEP,
                cancellationToken);
        }

        public ITftpPacket SendAndWaitForResponse(
            ITftpPacket sendPacket,
            IPEndPoint destinationEP,
            Func<ITftpPacket, bool> receiveFilter,
            out IPEndPoint receivedFromEP,
            CancellationToken cancellationToken)
        {
            return SendAndWaitForResponse(
                sendPacket,
                destinationEP,
                DefaultResponseTimeout,
                DefaultRetryPeriod,
                receiveFilter,
                out receivedFromEP,
                cancellationToken);
        }

        /* 
         * This method performs the following steps:
         *   1) send sendPacket to destinationEP every retryPeriod
         *   2) All packets for which receiveFilter returns false are discarded
         * This will continue until:
         *   a) receiveFilter returns true for a received packet - receivedFromEP is set to the 
         *      endpoint from which the packet was received and the packet is returned.
         *   b) timeout has elapsed - TimeoutException is thrown
         *   c) cancellationToken has been signalled - OperationCanceledException is thrown
         */
        public ITftpPacket SendAndWaitForResponse(
            ITftpPacket sendPacket,
            IPEndPoint destinationEP,
            TimeSpan timeout,
            TimeSpan retryPeriod,
            Func<ITftpPacket, bool> receiveFilter,
            out IPEndPoint receivedFromEP,
            CancellationToken cancellationToken)
        {
            ITftpPacket receivePacket = null;
            byte[] sendPacketBytes = sendPacket.Serialize();

            // Set up timeout cancellation token and link it with the cancellation token parameter.
            var timeoutCts = new CancellationTokenSource(timeout);
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Spin up a task thread to re-send sendPacket every retryPeriod.
            Action sendAction = () =>
            {
                while (true)
                {
                    _udp.Send(sendPacketBytes, destinationEP);
                    linkedCts.Token.WaitHandle.WaitOne(retryPeriod);
                    linkedCts.Token.ThrowIfCancellationRequested();
                    Console.WriteLine("Re-sending packet");
                }
            };
            var sendTask = Task.Factory.StartNew(sendAction, linkedCts.Token);

            try
            {
                receivePacket = Receive(receiveFilter, out receivedFromEP, linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Here we need to disambiguate which cancellation token source caused the OperationCanceledException.
                // If the cancellation token passed into this method was signalled,
                // then we need to let the OperationCanceledException propagate up as-is.
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("SendAndWaitForResponse was canceled.");
                    throw;
                }
                // Otherwise, the timeout cancellation token caused the OperationCanceledException.
                else
                {
                    Console.WriteLine("Receive timed out.");
                    throw new TimeoutException(string.Format("A response was not received after {0}.", timeout));
                }
            }

            // Cancel and clean up the send task.
            linkedCts.Cancel();
            try
            {
                sendTask.Wait();
            }
            catch (AggregateException ex)
            {
                ex.Handle(e => e is TaskCanceledException);
            }

            return receivePacket;
        }

        public void Connect(IPEndPoint remoteEP)
        {
            Console.WriteLine("Connecting port {0} to {1}", ((IPEndPoint)_udp.Client.LocalEndPoint).Port, remoteEP);
            lock (_defaultRemoteEPLock)
            {
                _defaultRemoteEP = remoteEP;
            }
        }

        public ITftpPacket Receive(Func<ITftpPacket, bool> receiveFilter, out IPEndPoint remoteEP, CancellationToken cancellationToken)
        {
            ReceiveResult result;
            lock (_receiveFilterLock)
                _receiveFilter = receiveFilter;
            result = _receivedPackets.Take(cancellationToken);
            lock (_receiveFilterLock)
                _receiveFilter = null;
            // Clear any received packets in case one was added before the receive filter was disabled.
            ReceiveResult dummy;
            _receivedPackets.TryTake(out dummy);
            remoteEP = result.RemoteEP;
            return result.Packet;
        }

        public int Send(byte[] bytes)
        {
            IPEndPoint remoteEP;
            lock (_defaultRemoteEPLock)
            {
                if (_defaultRemoteEP == null)
                    throw new InvalidOperationException("This operation is not valid before Connect has been called.");
                remoteEP = _defaultRemoteEP;
            }
            return _udp.Send(bytes, remoteEP);
        }

        public int Send(ITftpPacket packet)
        {
            return Send(packet.Serialize());
        }

        private class ReceiveResult
        {
            public ReceiveResult(ITftpPacket packet, IPEndPoint remoteEP)
            {
                _packet = packet;
                _remoteEP = remoteEP;
            }

            private ITftpPacket _packet;
            public ITftpPacket Packet { get { return _packet; } }

            private IPEndPoint _remoteEP;
            public IPEndPoint RemoteEP { get { return _remoteEP; } }
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
            {
                _udp.Close();
                _receivedPackets.Dispose();
            }
            _disposed = true;
        }
    }
}
