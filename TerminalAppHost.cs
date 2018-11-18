using Milkitic.ApplicationHost;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BstServer
{
    public class TerminalAppHost : AppHost
    {
        public List<SysInfo> SysInfos { get; set; } = new List<SysInfo>();
        public object LockObj = new object();

        public TerminalAppHost() : this(new HostSettings())
        {
        }

        public TerminalAppHost(HostSettings hostSettings) : this(null, hostSettings)
        {
        }

        public TerminalAppHost(string fileName)
            : this(fileName, new HostSettings())
        {
        }

        public TerminalAppHost(string fileName, HostSettings hostSettings)
            : this(fileName, null, hostSettings)
        {
        }

        public TerminalAppHost(string fileName, string args, HostSettings hostSettings)
            : base(fileName, args, hostSettings)
        {
            Run();
            DataReceived += (sender, e) =>
            {
                if (e.Data == null) return;
                try
                {
                    var array = e.Data.Split(';');
                    //File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\terminal.data", e.Data + "\r\n");
                    DateTimeOffset dt = new DateTimeOffset(new DateTime(long.Parse(array[0])));
                    float procCpu = float.Parse(array[1]);
                    long procMem = long.Parse(array[2]);
                    float sysCpu = float.Parse(array[3]);
                    long sysMem = long.Parse(array[4]);
                    long totalMem = long.Parse(array[5]);
                    lock (LockObj)
                    {
                        SysInfos.Add(new SysInfo(dt.ToUnixTimeMilliseconds(), procCpu, procMem, "scrds.exe"));
                        SysInfos.Add(new SysInfo(dt.ToUnixTimeMilliseconds(), sysCpu, sysMem, "All"));
                    }
                }
                catch
                {
                    // ignored
                }
            };
        }

        public static TerminalAppHost GetInstance()
        {
            return new TerminalAppHost(AppDomain.CurrentDomain.BaseDirectory + "\\terminal.exe",
                Process.GetCurrentProcess().Id + " srcds", new HostSettings { ShowWindow = false });
        }

        public class SysInfo
        {
            public SysInfo(long unixTime, float cpuUsage, long memoryUsed, string type)
            {
                UnixTime = unixTime;
                CpuUsage = cpuUsage;
                MemoryUsed = memoryUsed;
                Type = type;
            }

            [JsonProperty("time")]
            public long UnixTime { get; set; }
            [JsonProperty("cpu_usage")]
            public float CpuUsage { get; set; }
            [JsonProperty("memory")]
            public long MemoryUsed { get; set; }
            [JsonProperty("type")]
            public string Type { get; set; }
        }
    }
}
