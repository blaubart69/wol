using System;
using System.Net;

namespace wol
{
    public class NetMisc
    {
        public static IPAddress Broadcast(IPAddress IPv4, int netBits)
        {
            Int32 bcBits = IPAddress.HostToNetworkOrder((Int32)((1 << (32 - netBits)) - 1));

            Int32 ipBits = BitConverter.ToInt32(IPv4.GetAddressBytes(), 0);

            return new IPAddress(BitConverter.GetBytes(ipBits | bcBits));
        }
    }
}
