using System;
using System.Collections.Generic;

using Spi;

namespace wol
{
    class Opts
    {
        public string FilenameMacAddresses;
        public string FilenameBroadcastIPs;
        public string FilenameCIDRs;
        public string MAC;
        public string broadcastIP;
        public string cidr;
        public bool verbose = false;

        private Opts()
        {

        }
        public static bool ParseOpts(string[] args, out Opts opts)
        {
            opts = null;
            bool showhelp = false;

            Opts tmpOpts = new Opts() { };
            var cmdOpts = new BeeOptsBuilder()
                .Add('m',  "mac",           OPTTYPE.VALUE, "mac address for ", o => tmpOpts.MAC = o)
                .Add(null, "macfile",       OPTTYPE.VALUE, "input file with MAC addresses", o => tmpOpts.FilenameMacAddresses = o)
                .Add('s',  "broadcast",     OPTTYPE.VALUE, "IP address to send to", o => tmpOpts.broadcastIP = o)
                .Add(null, "broadcastfile", OPTTYPE.VALUE, "input file with subnet broadcast IPs", o => tmpOpts.FilenameBroadcastIPs = o)
                .Add('c',  "cidr",          OPTTYPE.VALUE, "CIDR of subnet", o => tmpOpts.cidr = o)
                .Add(null, "cidrfile",      OPTTYPE.VALUE, "input file with CIDRs", o => tmpOpts.FilenameCIDRs = o)
                .Add('v',  "verbose",       OPTTYPE.BOOL, "show some output", o => tmpOpts.verbose = true)
                .Add('h',  "help",          OPTTYPE.BOOL, "show help", o => showhelp = true)
                .GetOpts();

            var parsedArgs = BeeOpts.Parse(args, cmdOpts, (string unknownOpt) => Console.Error.WriteLine($"unknow option [{unknownOpt}]"));

            if ( String.IsNullOrEmpty(tmpOpts.MAC) && String.IsNullOrEmpty(tmpOpts.FilenameMacAddresses) )
            {
                Console.Error.WriteLine("no MAC of file with MACs given");
                showhelp = true;
            }
            if (   String.IsNullOrEmpty(tmpOpts.broadcastIP) && String.IsNullOrEmpty(tmpOpts.FilenameBroadcastIPs) 
                && String.IsNullOrEmpty(tmpOpts.cidr)        && String.IsNullOrEmpty(tmpOpts.FilenameCIDRs))
            {
                tmpOpts.broadcastIP = "255.255.255.255";
                if (tmpOpts.verbose)
                {
                    Console.WriteLine($"setting broadcast IP to {tmpOpts.broadcastIP} because no other options was specified");
                }
            }

            if (showhelp)
            {
                Console.WriteLine(
                      "\nusage: wol.exe [OPTIONS]"
                    + "\n\nOptions:");
                BeeOpts.PrintOptions(cmdOpts);
                return false;
            }

            opts = tmpOpts;
            return true;
        }
    }
}