using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace wol
{
    class wol
    {
        public delegate void OnSendSuccessfull(in IPAddress target);
        public delegate void OnSendError      (in IPAddress target, in string message);

        public static int SendEachMacToAllNets(
                            in IEnumerable<byte[]>              MACs, 
                            in IReadOnlyCollection<IPAddress>   broadcastIPs, 
                            int port,
                            in UdpClient v4Socket, in UdpClient v6Socket,
                            in OnSendSuccessfull onSendSuccessfull, in OnSendError onSendError,
                            bool verbose)
        {
            byte[] buffer = new byte[6 + 16 * 6];

            for (int i = 0; i < 6; ++i)
            {
                buffer[i] = 0xFF;
            }

            IReadOnlyCollection<IPEndPoint> targets = broadcastIPs.Select(ip => new IPEndPoint(ip, port)).ToList();

            int numberMacsSent = 0;

            foreach (byte[] mac in MACs)
            {
                // copy MAC address 16times to the buffer. offset 6!
                // MAC addresses are 6-byte (48-bits) in length
                for (int i = 0; i < 16; i++)
                {
                    mac.CopyTo(buffer, 6 + i * 6);
                }

                foreach (IPEndPoint broadcastIP in targets)
                {
                    UdpClient socketToUse = GetSocketForAddressFamily(v4Socket, v6Socket, broadcastIP.AddressFamily);

                    if (socketToUse != null)
                    {
                        if ( SendWOLpacket(buffer, broadcastIP, socketToUse, out string error, verbose))
                        {
                            onSendSuccessfull?.Invoke(broadcastIP.Address);
                            ++numberMacsSent;
                        }
                        else
                        {
                            onSendError?.Invoke(broadcastIP.Address, error);
                        }
                    }
                    else
                    {
                        onSendError?.Invoke(broadcastIP.Address, $"no socket available for address/family {broadcastIP.Address}/{broadcastIP.AddressFamily}");
                    }
                }
            }

            return numberMacsSent;
        }
        private static bool SendWOLpacket(in byte[] buffer, in IPEndPoint target, in UdpClient socketToUse, out string error, bool verbose)
        {
            try
            {
                error = null;
                socketToUse.Send(buffer, buffer.Length, target );
                if (verbose)
                {
                    Console.WriteLine($"I: sent from socket {socketToUse.Client.LocalEndPoint} to {target}");
                }
                return true;
            }
            catch (SocketException sox)
            {
                error = sox.Message;
                Console.WriteLine($"E: sent from socket {socketToUse.Client.LocalEndPoint} to {target}. {sox.Message}");
            }
            catch (InvalidOperationException ioex)
            {
                error = ioex.Message;
            }

            return false;
        }
        private static UdpClient GetSocketForAddressFamily(in UdpClient v4Socket, in UdpClient v6Socket, in AddressFamily family)
        {
            UdpClient socketToUse = null;
            if (family == AddressFamily.InterNetwork && v4Socket != null)
            {
                socketToUse = v4Socket;
            }
            else if (family == AddressFamily.InterNetworkV6 && v6Socket != null)
            {
                socketToUse = v6Socket;
            }

            return socketToUse;
        }
    }
}
