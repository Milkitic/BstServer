using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoCurrentDirectory
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < 10000000; i++)
            {
                string source = "150920";
                var array = source.SplitByStep(1);
            }

            string[] paths =
            {
                new FileInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName).DirectoryName,
                System.Environment.CurrentDirectory,
                System.IO.Directory.GetCurrentDirectory(),
                System.AppDomain.CurrentDomain.BaseDirectory,
                System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase
            };

            foreach (var path in paths)
            {
                Console.WriteLine(path);
            }
        }


    }

    public static class StringExtension
    {
        public static IEnumerable<string> SplitByStep(this string source, int step)
        {
            int len = source.Length;
            string[] array = new string[(int)Math.Ceiling((double)len / step)];
            for (int i = 0, j = 0; j < array.Length; i += step, j++)
            {
                if (i + step > len)
                    array[j] = source.Substring(i);
                else
                    array[j] = source.Substring(i, step);
            }

            return array;
        }
    }
}
