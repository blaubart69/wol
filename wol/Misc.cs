using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Spi
{
    class Misc
    {
        [ThreadStatic]
        static StringBuilder StrFormatByteSizeBuilder;
        const int StrFormatByteSizeBufferLen = 64;
        public static string StrFormatByteSize(long Filesize)
        {
            if (StrFormatByteSizeBuilder == null)
            {
                StrFormatByteSizeBuilder = new StringBuilder(StrFormatByteSizeBufferLen);
            }

            StrFormatByteSize(Filesize, StrFormatByteSizeBuilder, StrFormatByteSizeBufferLen);
            return StrFormatByteSizeBuilder.ToString();
        }
        [DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern long StrFormatByteSize(
               long fileSize
               , [MarshalAs(UnmanagedType.LPTStr)] StringBuilder buffer
               , int bufferSize);

        public static IEnumerable<string> ConcatFilecontentAndOneValue(string value, string filename)
        {
            if (!String.IsNullOrEmpty(value))
            {
                yield return value;
            }

            if (!String.IsNullOrEmpty(filename))
            {
                foreach (string line in File.ReadLines(filename))
                {
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        yield return line;
                    }
                }
            }
        }
        public static string NiceDuration(TimeSpan ts)
        {
            StringBuilder res = new StringBuilder(capacity: 32);

            if (ts.TotalHours >= 24)
            {
                //res = String.Format("{0}d {1}h {2}m {3}s {4}ms", ts.Days, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                res.Append(ts.Days);
                res.Append("d ");
            }
            if (ts.TotalMinutes >= 60)
            {
                res.Append(ts.Hours);
                res.Append("h ");
                //res = String.Format("{0}h {1}m {2}s {3}ms", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            }
            if (ts.TotalSeconds >= 60)
            {
                res.Append(ts.Minutes);
                res.Append("m ");
                //res = String.Format("{0}m {1}s {2}ms", ts.Minutes, ts.Seconds, ts.Milliseconds);
            }
            if (ts.TotalMilliseconds >= 1000)
            {
                res.Append(ts.Seconds);
                res.Append("s ");
                //res = String.Format("{0}s {1}ms", ts.Seconds, ts.Milliseconds);
            }
            //res = String.Format("{0}ms", ts.Milliseconds);
            res.Append(ts.Milliseconds);
            res.Append("ms");

            return res.ToString();
        }
    }
}
