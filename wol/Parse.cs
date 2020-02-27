using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace wol
{
    class Parse
    {
        public delegate void OnParseError(string item, string message);
        public static IEnumerable<IPAddress> IPs(IEnumerable<string> IPs, OnParseError onParseError)
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
                    onParseError?.Invoke(stringIP, fex.Message);
                }
                if (ip != null)
                {
                    yield return ip;
                }
            }
        }
        public static IEnumerable<byte[]> MACs(IEnumerable<string> stringMACs, OnParseError onParseError)
        {
            foreach (string stringMAC in stringMACs)
            {
                byte[] parsedMAC = null;

                string tmpMAC = stringMAC.ToUpper().Replace(":", "-");
                try
                {
                    parsedMAC = PhysicalAddress.Parse(tmpMAC).GetAddressBytes();
                    if (parsedMAC.Length != 6)
                    {
                        onParseError?.Invoke(stringMAC, $"E: MAC [{stringMAC}] has a length of {parsedMAC.Length}. Should be 6.");
                        parsedMAC = null;
                    }
                }
                catch (FormatException fex)
                {
                    onParseError?.Invoke(tmpMAC, $"E: MAC [{stringMAC}] could not be parsed. Was transformed to [{tmpMAC}]. Exception: {fex.Message}");
                    parsedMAC = null;
                }

                if (parsedMAC != null)
                {
                    yield return parsedMAC;
                }
            }
        }
        public static IEnumerable<IPAddress> CIDRs(IEnumerable<string> CIDRs, OnParseError onParseError)
        {
            foreach (string cidr in CIDRs)
            {
                IPAddress broadcast = null;

                try
                {
                    var network = IPNetwork.Parse(cidr);
                    broadcast = network.Broadcast;
                }
                catch (Exception ex)
                {
                    onParseError?.Invoke(cidr, ex.Message);
                }

                if (broadcast != null)
                {
                    yield return broadcast;
                }
            }
        }

    }
}
