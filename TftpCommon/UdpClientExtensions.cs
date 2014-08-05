using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Tftp
{
    public static class UdpClientExtensions
    {
        public static int Send(this UdpClient udp, byte[] dgram)
        {
            return udp.Send(dgram, dgram.Length);
        }

        public static int Send(this UdpClient udp, byte[] dgram, IPEndPoint endPoint)
        {
            return udp.Send(dgram, dgram.Length, endPoint);
        }
    }
}
