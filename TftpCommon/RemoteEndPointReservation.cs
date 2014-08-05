using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net;
using System.Net.Sockets;

namespace Tftp
{
    public class RemoteEndPointReservation : IDisposable
    {
        private static HashSet<string> _usedRemoteEPs = new HashSet<string>();

        public IPEndPoint EndPoint { get; private set; }

        public RemoteEndPointReservation(IPEndPoint endPoint)
        {
            string endPointString = endPoint.ToString();
            lock (_usedRemoteEPs)
            {
                if (_usedRemoteEPs.Contains(endPointString))
                {
                    Console.WriteLine("The remote endpoint {0} is already in use by another TFTP connection.", endPointString);
                    throw new SocketException((int)SocketError.AddressAlreadyInUse);
                }
                _usedRemoteEPs.Add(endPointString);
            }
            EndPoint = endPoint;
        }

        public void Dispose()
        {
            string endPointString = EndPoint.ToString();
            lock (_usedRemoteEPs)
            {
                _usedRemoteEPs.Remove(endPointString);
            }
        }
    }
}
