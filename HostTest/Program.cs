using BstServer;
using Milkitic.ApplicationHost;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HostTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string path =
                @"D:\repos\BstServer\EchoCurrentDirectory\bin\Debug\EchoCurrentDirectory.exe";
            //path = "cmd";
            L4D2AppHost host = new L4D2AppHost(path, new HostSettings
            {
                ShowWindow = false,
                Encoding = Encoding.GetEncoding(936),
                RedirectStandardInput = false
            });
            host.DataReceived += Host_DataReceived;
            host.Run();
            //host.UserConfig.SteamUsers.Add(new BstServer.Models.SteamUser());
            //var d = user.GetDayInfo(18, 11, 17);
            //var m = user.GetMonthInfo(18, 12);
            //var y = user.GetYearInfo(18);
            //double ssb = host.UserConfig.GetUserRate(user,18,11,16);
            //var ssb = y.DamageList;
            //var sbb = d.DamageList;
            //var list = File.ReadAllLines(@"C:\Users\YureruMiira\Desktop\l4dinfos.txt");
            //foreach (var s in list)
            //{
            //    host._queue.Enqueue(s);
            //}
            Console.Read();
            return;
            //host.DataReceived += Host_DataReceived;
            //host.Run();
            var task = new Task(() =>
            {
                Thread.CurrentThread.IsBackground = false;
                host.WaitForExit();
                Console.WriteLine("Process has exit. Press any key to continue...");
                Console.ReadKey();
            });
            task.Start();

            if (host.CanSendMessage)
                while (true)
                {
                    var sb = Console.ReadLine();
                    host.SendMessage(sb);
                }
            else
                host.WaitForExit();
        }

        private static void Host_DataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
    }
}
