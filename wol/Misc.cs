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
        public static string NiceDuration(TimeSpan duration)
        {
            string nice = String.Empty;

            if (duration.Ticks >= TimeSpan.TicksPerDay)
            {
                nice += duration.Days + "d ";
            }
            if (duration.Ticks >= TimeSpan.TicksPerHour)
            {
                nice += duration.Hours + "h ";
            }
            if (duration.Ticks >= TimeSpan.TicksPerMinute)
            {
                nice += duration.Minutes + "m ";
            }
            if (duration.Ticks >= TimeSpan.TicksPerSecond)
            {
                nice += duration.Seconds + "s ";
            }

            return nice + duration.Milliseconds + "ms";
        }
    }
}
