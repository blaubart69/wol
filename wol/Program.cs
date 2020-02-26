using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

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
        static int Main(string[] args)
        {
            Opts opts;
            if (!Opts.ParseOpts(args, out opts))
            {
                return 99;
            }

            Stats stats = new Stats();

            IEnumerable<byte[]> MACs = GetMACs(opts.MAC, opts.FilenameMacAddresses);
            List<IPEndPoint> broadcastIPs = GetIPs(opts.broadcastIP, opts.FilenameBroadcastIPs).Select(ip => new IPEndPoint(ip, 7)).ToList();
            
            if (opts.verbose)
            {
                Console.WriteLine($"sending each MAC to {broadcastIPs.Count} IPs");
            }
            
            SendToAllNets(opts, stats, MACs, broadcastIPs);
            WriteStats(stats);
            return stats.errors == 0 ? 0 : 8;
        }

        private static void WriteStats(Stats stats)
        {
            long onePacketSize = 6 + 6 * 16;
            long bytesSent = onePacketSize * stats.sentPackets;
            Console.WriteLine(
                $"sent {stats.sentPackets} WOL packets for {stats.numberMACs} MACs."
              + $" size all packets: {Spi.Misc.StrFormatByteSize(bytesSent)}"
              + $" errors: {stats.errors}");
        }

        private static void SendToAllNets(Opts opts, Stats stats, IEnumerable<byte[]> MACs, List<IPEndPoint> broadcastIPs)
        {
            using (UdpClient udpClient = new UdpClient())
            {
                byte[] buffer = new byte[6 + 16 * 6];

                for (int i = 0; i < 6; ++i)
                {
                    buffer[i] = 0xFF;
                }

                foreach (byte[] mac in MACs)
                {
                    if (mac == null)
                    {
                        stats.errors++;
                        continue;
                    }
                    SendToNets(udpClient, ref buffer, mac, broadcastIPs);
                    stats.sentPackets += broadcastIPs.Count;
                    stats.numberMACs += 1;

                    if (opts.verbose)
                    {
                        Console.WriteLine(BitConverter.ToString(mac));
                    }
                }
            }
        }

        private static void SendToNets(UdpClient udpClient, ref byte[] buffer, in byte[] mac, List<IPEndPoint> broadcastIPs)
        {

            // MAC addresses are 6-byte (48-bits) in length
            foreach ( var ip in broadcastIPs )
            {
                for (int i = 0; i < 16; i++)
                {
                    mac.CopyTo(buffer, 6 + i * 6);
                }

                udpClient.Send(buffer, buffer.Length, ip);
            }
        }
        private static IEnumerable<IPAddress> GetIPs(string broadcastIP, string filenameBroadcastIPs)
        {
            if ( !String.IsNullOrEmpty(broadcastIP) )
            {
                yield return IPAddress.Parse(broadcastIP);
            }

            if ( !String.IsNullOrEmpty(filenameBroadcastIPs) )
            {
                using (TextReader rdr = new StreamReader(filenameBroadcastIPs))
                {
                    string line;
                    while ((line = rdr.ReadLine()) != null)
                    {
                        yield return IPAddress.Parse(line);
                    }
                }
            }
        }

        private static IEnumerable<byte[]> GetMACs(string MAC, string filenameMacAddresses)
        {
            if (!String.IsNullOrEmpty(MAC))
            {
                yield return StringToMAC(MAC);
            }
            
            if ( !String.IsNullOrEmpty(filenameMacAddresses) )
            {
                using (TextReader rdr = new StreamReader(filenameMacAddresses))
                {
                    string line;
                    while ((line=rdr.ReadLine()) != null)
                    {
                        yield return StringToMAC(line);
                    }
                }
            }
        }

        private static byte[] StringToMAC(string MAC)
        {
            string tmpMAC = MAC.ToUpper().Replace(":", "-");
            try
            {
                byte[] macBytes = PhysicalAddress.Parse(tmpMAC).GetAddressBytes();
                if ( macBytes.Length != 6 )
                {
                    Console.Error.WriteLine($"MAC [{MAC}] has a length of {macBytes.Length}. Should be 6.");
                    return null;
                }
                return macBytes;
            }
            catch (FormatException fex)
            {
                Console.Error.WriteLine($"MAC [{MAC}] could not be parsed. Was transformed to [{tmpMAC}]. Exception: {fex.Message}");
            }
            return null;
        }
    }
}
