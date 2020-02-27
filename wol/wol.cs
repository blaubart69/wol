using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace wol
{
    class wol
    {
        public delegate void OnSendSuccessfull(in byte[] mac, in IPEndPoint target);
        public delegate void OnSendError      (in byte[] mac, in IPEndPoint target, in string message);

        public static int SendToAllNets(IEnumerable<byte[]> MACs, IList<IPEndPoint> broadcastIPs, 
                            UdpClient v4Socket, UdpClient v6Socket,
                            OnSendSuccessfull onSendSuccessfull, OnSendError onSendError)
        {
            byte[] buffer = new byte[6 + 16 * 6];

            for (int i = 0; i < 6; ++i)
            {
                buffer[i] = 0xFF;
            }

            int numberMacs = 0;

            foreach (byte[] mac in MACs)
            {
                ++numberMacs;

                // copy MAC address 16times to the buffer. offset 6!
                // MAC addresses are 6-byte (48-bits) in length
                for (int i = 0; i < 16; i++)
                {
                    mac.CopyTo(buffer, 6 + i * 6);
                }

                foreach (IPEndPoint broadcastIP in broadcastIPs)
                {
                    UdpClient socketToUse = GetSocketForAddressFamily(v4Socket, v6Socket, broadcastIP);

                    if (socketToUse != null)
                    {
                        if ( SendWOLpacket(buffer, broadcastIP, socketToUse, out string error))
                        {
                            onSendSuccessfull?.Invoke(mac, broadcastIP);
                        }
                        else
                        {
                            onSendError?.Invoke(mac, broadcastIP, error);
                        }
                    }
                    else
                    {
                        onSendError?.Invoke(mac, broadcastIP, $"no socket available for address/family {broadcastIP.Address}/{broadcastIP.AddressFamily}");
                    }
                }
            }

            return numberMacs;
        }
        private static bool SendWOLpacket(byte[] buffer, IPEndPoint broadcastIP, UdpClient socketToUse, out string error)
        {
            try
            {
                error = null;
                socketToUse.Send(buffer, buffer.Length, broadcastIP);
                return true;
            }
            catch (SocketException sox)
            {
                error = sox.Message;
            }
            catch (InvalidOperationException ioex)
            {
                error = ioex.Message;
            }

            return false;
        }
        private static UdpClient GetSocketForAddressFamily(UdpClient v4Socket, UdpClient v6Socket, IPEndPoint broadcastIP)
        {
            UdpClient socketToUse = null;
            if (broadcastIP.AddressFamily == AddressFamily.InterNetwork && v4Socket != null)
            {
                socketToUse = v4Socket;
            }
            else if (broadcastIP.AddressFamily == AddressFamily.InterNetworkV6 && v6Socket != null)
            {
                socketToUse = v6Socket;
            }

            return socketToUse;
        }
    }
}
