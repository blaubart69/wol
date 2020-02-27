using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

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

                IEnumerable<byte[]> MACs = ParseMACs(Misc.ConcatFilecontentAndOneValue(opts.MAC, opts.FilenameMacAddresses),
                                                                    OnParseError: (string MAC) => stats.errors++);

                List<IPEndPoint> broadcastIPs = ParseIPs(Misc.ConcatFilecontentAndOneValue(opts.broadcastIP, opts.FilenameBroadcastIPs),
                                                                    OnParseError: (string IP) => stats.errors++)
                                                      .Select(ip => new IPEndPoint(ip, WOL_UDP_PORT)).ToList();

                if (opts.verbose)
                {
                    Console.WriteLine($"sending each MAC to {broadcastIPs.Count} IPs");
                }

                CreateSockets(broadcastIPs, out UdpClient v4Socket, out UdpClient v6Socket, opts.verbose);

                DateTime start = DateTime.Now;
                SendToAllNets(opts, stats, MACs, broadcastIPs, v4Socket, v6Socket);
                TimeSpan duration = DateTime.Now - start;

                if (v4Socket != null) v4Socket.Close();
                if (v6Socket != null) v6Socket.Close();

                WriteStats(stats, broadcastIPs.Count, duration);

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

                // MAC addresses are 6-byte (48-bits) in length
                foreach (var broadcastIP in broadcastIPs)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        mac.CopyTo(buffer, 6 + i * 6);
                    }
                    UdpClient socketToUse = null;
                    if (broadcastIP.AddressFamily == AddressFamily.InterNetwork && v4Socket != null)
                    {
                        socketToUse = v4Socket;
                    }
                    else if (broadcastIP.AddressFamily == AddressFamily.InterNetworkV6 && v6Socket != null) 
                    {
                        socketToUse = v6Socket;
                    }

                    if (socketToUse != null)
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
                }

                if (opts.verbose)
                {
                    Console.WriteLine(BitConverter.ToString(mac));
                }
            }
        }
        private static IEnumerable<IPAddress> ParseIPs(IEnumerable<string> IPs, Action<string> OnParseError)
        {
            foreach (string stringIP in IPs)
            {
                IPAddress ip = null;
                try
                {
                    ip = IPAddress.Parse(stringIP);
                }
                catch (FormatException fex)
                {
                    Console.Error.WriteLine($"E: IP [{stringIP}] could not be parsed. [{fex.Message}]");
                }
                if (ip != null)
                {
                    yield return ip;
                }
                else
                {
                    OnParseError?.Invoke(stringIP);
                }
            }
        }
        private static IEnumerable<byte[]> ParseMACs(IEnumerable<string> stringMACs, Action<string> OnParseError)
        {
            foreach ( string stringMAC in stringMACs)
            {
                byte[] parsedMAC = null;

                string tmpMAC = stringMAC.ToUpper().Replace(":", "-");
                try
                {
                    parsedMAC = PhysicalAddress.Parse(tmpMAC).GetAddressBytes();
                    if (parsedMAC.Length != 6)
                    {
                        Console.Error.WriteLine($"E: MAC [{stringMAC}] has a length of {parsedMAC.Length}. Should be 6.");
                        parsedMAC = null;
                    }
                }
                catch (FormatException fex)
                {
                    Console.Error.WriteLine($"E: MAC [{stringMAC}] could not be parsed. Was transformed to [{tmpMAC}]. Exception: {fex.Message}");
                    parsedMAC = null;
                }

                if ( parsedMAC != null )
                {
                    yield return parsedMAC;
                }
                else
                {
                    OnParseError?.Invoke(stringMAC);
                }
            }
        }
    }
}
