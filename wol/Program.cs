using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using Spi;

namespace wol
{
    class Stats
    {
        public long sentPackets;
        public long numberMACs;
        public long errors;
    }
    class Program
    {
        const int WOL_UDP_PORT = 7;
        static int Main(string[] args)
        {
            try
            {
                Opts opts;
                if (!Opts.ParseOpts(args, out opts))
                {
                    return 99;
                }

                Stats stats = new Stats();

                IEnumerable<byte[]> MACs = 
                    Parse.MACs(Misc.ConcatFilecontentAndOneValue(opts.MAC, opts.FilenameMacAddresses),
                        onParseError: (string MAC, string message) =>
                        {
                            ++stats.errors;
                            Console.Error.WriteLine($"could not parse MAC [{MAC}]. [{message}]");
                        });

                IEnumerable<IPAddress> broadcastIPsFromCidrs = 
                    Parse.CIDRs(Misc.ConcatFilecontentAndOneValue(opts.cidr, opts.FilenameCIDRs),
                        onParseError: (string CIDR, string message) =>
                        {
                            ++stats.errors;
                            Console.Error.WriteLine($"could not parse CIDR [{CIDR}]. [{message}]");
                        });

                IEnumerable<IPAddress> broadcastIPsFromFile = 
                    Parse.IPs(Misc.ConcatFilecontentAndOneValue(opts.broadcastIP, opts.FilenameBroadcastIPs),
                        onParseError: (string IP, string message) =>
                        { 
                            ++stats.errors;
                            Console.Error.WriteLine($"could not parse IP [{IP}]. [{message}]");
                        });
                                                      

                List<IPEndPoint> broadcastEndpoints = broadcastIPsFromCidrs.Concat(broadcastIPsFromFile)
                    .Select(ip => new IPEndPoint(ip, WOL_UDP_PORT)).ToList();

                if (opts.verbose)
                {
                    foreach (var ip in broadcastEndpoints )
                    {
                        Console.WriteLine($"broadcast IP: {ip}");
                    }
                    Console.WriteLine($"sending each MAC to {broadcastEndpoints.Count} IPs");
                }

                CreateSockets(broadcastEndpoints, out UdpClient v4Socket, out UdpClient v6Socket, opts.verbose);

                DateTime start = DateTime.Now;
                SendToAllNets(opts, stats, MACs, broadcastEndpoints, v4Socket, v6Socket);
                TimeSpan duration = DateTime.Now - start;

                if (v4Socket != null) v4Socket.Close();
                if (v6Socket != null) v6Socket.Close();

                WriteStats(stats, broadcastEndpoints.Count, duration);

                return stats.errors == 0 ? 0 : 8;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return 12;
            }
        }
        private static void CreateSockets(in List<IPEndPoint> IPs, out UdpClient v4Socket, out UdpClient v6Socket, bool verbose)
        {
            Func<AddressFamily,UdpClient> createSocket = (AddressFamily family) =>
            {
                try
                {
                    var socket = new UdpClient(family);
                    if (verbose)
                    {
                        Console.WriteLine($"socket for family {family} created");
                    }
                    return socket;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"could not create socket for AddressFamily {family}. [{ex.Message}]");
                }
                return null;
            };

            v4Socket = null;
            v6Socket = null;

            if (IPs.Any(ip => ip.AddressFamily == AddressFamily.InterNetwork))
            {
                v4Socket = createSocket(AddressFamily.InterNetwork);   
            }

            if (IPs.Any(ip => ip.AddressFamily == AddressFamily.InterNetworkV6))
            {
                v6Socket = createSocket(AddressFamily.InterNetworkV6);
            }
        }
        private static void SendToAllNets(Opts opts, Stats stats, IEnumerable<byte[]> MACs, List<IPEndPoint> broadcastIPs, UdpClient v4Socket, UdpClient v6Socket)
        {
            byte[] buffer = new byte[6 + 16 * 6];

            for (int i = 0; i < 6; ++i)
            {
                buffer[i] = 0xFF;
            }

            foreach (byte[] mac in MACs)
            {
                stats.numberMACs += 1;

                foreach (var broadcastIP in broadcastIPs)
                {
                    // copy MAC address 16times to the buffer. offset 6!
                    // MAC addresses are 6-byte (48-bits) in length
                    for (int i = 0; i < 16; i++)
                    {
                        mac.CopyTo(buffer, 6 + i * 6);
                    }

                    UdpClient socketToUse = GetSocketForAdressFamily(v4Socket, v6Socket, broadcastIP);

                    if (socketToUse != null)
                    {
                        SendWOLpacket(stats, buffer, broadcastIP, socketToUse);
                    }
                }

                if (opts.verbose)
                {
                    Console.WriteLine(BitConverter.ToString(mac));
                }
            }
        }
        private static void SendWOLpacket(Stats stats, byte[] buffer, IPEndPoint broadcastIP, UdpClient socketToUse)
        {
            try
            {
                socketToUse.Send(buffer, buffer.Length, broadcastIP);
                ++stats.sentPackets;
            }
            catch (SocketException sox)
            {
                ++stats.errors;
                Console.Error.WriteLine(sox.Message);
            }
            catch (InvalidOperationException ioex)
            {
                ++stats.errors;
                Console.Error.WriteLine(ioex.Message);
            }
        }
        private static UdpClient GetSocketForAdressFamily(UdpClient v4Socket, UdpClient v6Socket, IPEndPoint broadcastIP)
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
        private static void WriteStats(Stats stats, int numberSubnetIPs, TimeSpan duration)
        {
            long onePacketSize = 6 + 6 * 16;
            long bytesSent = onePacketSize * stats.sentPackets;

            string packetPerS = duration.TotalSeconds == 0 ? "n/a" : ((double)stats.sentPackets / duration.TotalSeconds).ToString();

            Console.WriteLine(
                $"\nnumber MAC addresses: {stats.numberMACs}"
              + $"\nnumber subnet IPs:    {numberSubnetIPs}"
              + $"\nWOL packets sent:     {stats.sentPackets}"
              + $"\npayload sent:         {Spi.Misc.StrFormatByteSize(bytesSent)}"
              + $"\npacket/s:             {packetPerS}"
              + $"\nduration:             {Misc.NiceDuration(duration)}"
              + $"\nerrors:               {stats.errors}"
              );
        }

    }
}
