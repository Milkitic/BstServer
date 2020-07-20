using System;
using System.Collections.Generic;
using System.IO;

namespace EchoCurrentDirectory
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] paths =
            {

            };

            foreach (var path in paths)
            {
                Console.WriteLine(path);
            }

            Console.WriteLine(Path.GetDirectoryName("CurrentProcess.MainModule: " +
                                                    System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName));
            Console.WriteLine("System.Environment.CurrentDirectory: " + System.Environment.CurrentDirectory);
            Console.WriteLine("System.IO.Directory.GetCurrentDirectory(): " + System.IO.Directory.GetCurrentDirectory());
            Console.WriteLine("System.AppDomain.CurrentDomain.BaseDirectory: " +
                              System.AppDomain.CurrentDomain.BaseDirectory);
            Console.WriteLine("System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase: " +
                              System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
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
