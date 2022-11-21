using System.Collections.Concurrent;
using BstServer.Models;
using Milkitic.ApplicationHost;
using Newtonsoft.Json;

namespace BstServer;

public class L4D2AppHost : AppHost
{
    private static readonly string CurrentPath = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string ConfigFilePath = Path.Combine(CurrentPath, "user.json");

    private readonly Task _scanTask;
    public readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
    private readonly CancellationTokenSource _cts;

    public UserConfig UserConfig { get; set; }
    private readonly List<string> _holdingUsers = new List<string>();

    public L4D2AppHost() : this(new HostSettings())
    {
    }

    public L4D2AppHost(HostSettings hostSettings) : this(null, hostSettings)
    {
    }

    public L4D2AppHost(string fileName)
        : this(fileName, new HostSettings())
    {
    }

    public L4D2AppHost(string fileName, HostSettings hostSettings)
        : this(fileName, null, hostSettings)
    {
    }

    public L4D2AppHost(string fileName, string args, HostSettings hostSettings)
        : base(fileName, args, hostSettings)
    {
        _cts = new CancellationTokenSource();
        _scanTask = Task.Run(new Action(Scan));
        if (File.Exists(ConfigFilePath))
        {
            //JsonSerializerSettings settings = new JsonSerializerSettings();
            //settings.ObjectCreationHandling = ObjectCreationHandling.Reuse;
            UserConfig = JsonConvert.DeserializeObject<UserConfig>(File.ReadAllText(ConfigFilePath));
        }
        else
            UserConfig = new UserConfig { StartTime = new CustomDate(DateTime.Now).ToString() };

        //RunMissingDayFix();
    }

    public override void Run()
    {
        base.Run();
        DataReceived += L4D2AppHost_DataReceived;
        Exited += (sender, e) => { Clear(); };
    }

    public override void Stop()
    {
        base.Stop();
        DataReceived -= L4D2AppHost_DataReceived;
    }

    private void L4D2AppHost_DataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
    {
        _queue.Enqueue(e.Data);
    }

    private void Scan()
    {
        while (!_cts.IsCancellationRequested)
        {
            while (!_queue.IsEmpty)
            {
                if (_queue.TryDequeue(out var current))
                {
                    Parse(current);
                }
            }

            Thread.Sleep(10);
        }
    }

    private void Parse(string source)
    {
        if (source == null) return;
        try
        {
            if (DetectLogin(source)) return;
            if (AuthUser(source)) return;
            if (DetectExit(source)) return;
            if (DetectFriendlyFire(source)) return;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private bool DetectFriendlyFire(string source)
    {
        const string blackKey1 = ": [l4d_ff_tracker.smx] STEAM_";
        const string blackKey2 = " => ";

        var blackI1 = source.IndexOf(blackKey1, StringComparison.Ordinal);
        if (blackI1 == -1) return false;
        var blackI2 = source.IndexOf(blackKey2, StringComparison.Ordinal);
        if (blackI2 == -1) return false;
        var steamUid = source.Substring(blackKey1.Length + blackI1 - 6, blackI2 - (blackKey1.Length + blackI1 - 6));
        var user = UserConfig.SteamUsers.FirstOrDefault(k => k.SteamUid == steamUid && k.IsOnline);
        bool isValid = false;
        if (user == null) return false;

        var info = source.Substring(blackI2 + 4);
        var infoArray = info.Split(" [").Select(k => k.Trim(']')).ToArray();
        string botOrUid = infoArray[0];
        string weapon = infoArray[1];
        int damage = int.Parse(infoArray[2].Split(' ')[0]);
        bool isBot = botOrUid == "BOT";
        SteamUser user2 = null;
        if (!isBot)
        {
            user2 = UserConfig.SteamUsers.FirstOrDefault(k => k.SteamUid == botOrUid && k.IsOnline);
            if (user2 != null) isValid = true;
        }
        else
        {
            Console.WriteLine($"FF: {user.CurrentName} =({weapon})> BOT [{damage} HP]");
        }

        if (!isValid) return false;
        var nowCustomDate = new CustomDate(DateTime.Now);
        DailySteamUserInfo user1Today = GetTodayInfo(user, nowCustomDate); //user1 today info (create if not exist)
        DailySteamUserInfo user2Today = GetTodayInfo(user2, nowCustomDate); //user2 today info

        GetWeapon(weapon, user1Today.WeaponInfos).Damage += damage; //1 damage (create if not exist)
        GetWeapon(weapon, user2Today.WeaponInfos).Hurt += damage; //2 hurt

        GetWeapon(weapon, user1Today.WeaponInfos).DamageTimes += 1; //1 damage count
        GetWeapon(weapon, user2Today.WeaponInfos).HurtTimes += 1; //2 hurt count

        Console.WriteLine($"FF: {user.CurrentName} =({weapon})> {user2.CurrentName} [{damage} HP]");

        return true;
    }

    private bool DetectExit(string source)
    {
        const string exitKey1 = "Dropped ";
        const string exitKey2 = " from server (";

        if (!source.StartsWith(exitKey1)) return false;
        var index = source.IndexOf(exitKey2, StringComparison.Ordinal);
        if (index == -1) return false;
        var username = source.Substring(exitKey1.Length, index - exitKey1.Length);
        var user = UserConfig.SteamUsers.FirstOrDefault(k => k.CurrentName == username && k.IsOnline);
        if (user == null) return false;
        UpdateDisconnectInfo(user);
        SaveConfig();
        return true;
    }

    private bool DetectLogin(string source)
    {
        const string loginKey1 = "Client \"";
        const string loginKey2 = "\" connected (";
        if (!source.StartsWith(loginKey1)) return false;
        var index = source.IndexOf(loginKey2, StringComparison.Ordinal);
        if (index == -1) return false;

        var username = source.Substring(loginKey1.Length, index - loginKey1.Length);
        if (_holdingUsers.Contains(username)) return false;

        _holdingUsers.Add(username);
        Console.WriteLine("Holding user: " + username);
        return true;
    }

    private bool AuthUser(string source)
    {
        const string authKey1 = "[l4d_ff_tracker.smx] Detected client ";
        const string authKey2 = " with authenciation ";

        var index1 = source.IndexOf(authKey1, StringComparison.Ordinal);
        if (index1 == -1) return false;
        var index2 = source.IndexOf(authKey2, StringComparison.Ordinal);
        if (index2 == -1) return false;

        var username = source.Substring(authKey1.Length + index1, index2 - authKey1.Length - index1);
        if (!_holdingUsers.Contains(username)) return false;

        var steamUid = source.Substring(index2 + authKey2.Length).Trim();
        var user = UserConfig.SteamUsers.FirstOrDefault(k => k.SteamUid == steamUid);
        if (user == null)
        {
            user = new SteamUser();
            UserConfig.SteamUsers.Add(user);
            Console.WriteLine($"Add user: {username} ({steamUid})");
        }

        user.SteamUid = steamUid;
        user.CurrentName = username;
        user.LastConnect = DateTime.Now;
        user.IsOnline = true;
        Console.WriteLine($"ONLINE: {username} ({steamUid}) {user.LastConnect}");
        SaveConfig();

        _holdingUsers.Remove(username);
        return true;
    }

    private static WeaponCell GetWeapon(string weapon, ICollection<WeaponCell> dmg)
    {
        WeaponCell weaponDmg = dmg.FirstOrDefault(k => k.WeaponName == weapon);
        if (weaponDmg != null) return weaponDmg;

        dmg.Add(new WeaponCell(weapon));
        weaponDmg = dmg.First(k => k.WeaponName == weapon);
        return weaponDmg;
    }

    private static DailySteamUserInfo GetTodayInfo(SteamUser user, CustomDate nowCustomDate)
    {
        DailySteamUserInfo today =
            user.DailyInfos.FirstOrDefault(k => k.CustomDate == nowCustomDate);
        if (today != null) return today;

        user.DailyInfos.Add(new DailySteamUserInfo(nowCustomDate));
        today = user.DailyInfos.First(k => k.CustomDate == nowCustomDate);
        return today;
    }


    private void RunMissingDayFix()
    {
        Task.Run(() =>
        {
            while (true)
            {
                Random rnd = new Random();
                foreach (var user in UserConfig.SteamUsers)
                {
                    var date = new CustomDate(DateTime.Now);
                    var startDate = CustomDate.Parse(UserConfig.StartTime);
                    //var startDate = new CustomDate(DateTime.Now.AddDays(-2));
                    var y = startDate.Year;
                    var m = startDate.Month;
                    var d = startDate.Day;
                    while (y < date.Year ||
                           y == date.Year && m < date.Month ||
                           y == date.Year && m == date.Month && d <= date.Day)
                    {
                        _ = GetTodayInfo(user, new CustomDate(y, m, d));
                        //var sb = GetTodayInfo(user, new CustomDate(y, m, d));
                        //sb.HurtList.Add(new WeaponCell("Chrome Shotgun", rnd.Next(1, 101)));
                        //sb.HurtTimesList.Add(new WeaponCell("Chrome Shotgun", rnd.Next(1, 30)));
                        //sb.DamageList.Add(new WeaponCell("Chrome Shotgun", rnd.Next(1, 101)));
                        //sb.DamageTimesList.Add(new WeaponCell("Chrome Shotgun", rnd.Next(1, 30)));
                        //sb.OnlineTime = rnd.NextDouble() * 100;
                        //sb.SupportedYuan = rnd.Next(10);

                        var dt = new DateTime(y, m, d).AddDays(1);
                        y = dt.Year;
                        m = dt.Month;
                        d = dt.Day;
                    }
                }

                SaveConfig();
                Thread.Sleep((int)TimeSpan.FromDays(1).TotalMilliseconds);
            }

        });

    }

    //private void SetRate()
    //{
    //    string currentDate = GetCustomDate(DateTime.Now);
    //    var nowDateInfo = GetDate(currentDate);
    //    var dateInfo = GetDate(UserConfig.StartTime);
    //    var offsetY = dateInfo.Year;
    //    var offsetM = dateInfo.Month;
    //    while (offsetY < nowDateInfo.Year ||
    //           offsetY == nowDateInfo.Year && offsetM <= nowDateInfo.Month)
    //    {
    //        string key = GetCustomDate(new DateInfo(offsetY, offsetM));

    //        double total = UserConfig.SteamUsers.Select(k =>
    //            (double)k.DailyInfos[key].SupportedYuan / k.DailyInfos[key].OnlineTime).Sum();
    //        foreach (var user in UserConfig.SteamUsers)
    //        {
    //            user.DailyInfos[key].Rate =
    //                ((double)user.DailyInfos[key].SupportedYuan / user.DailyInfos[key].OnlineTime) / total;
    //        }

    //        offsetM++;
    //        if (offsetM == 13)
    //        {
    //            offsetM = 1;
    //            offsetY += 1;
    //        }
    //    }
    //}

    private static void UpdateDisconnectInfo(SteamUser user)
    {
        user.LastDisconnect = DateTime.Now;
        user.IsOnline = false;
        var date = new CustomDate(DateTime.Now);
        var time = user.LastDisconnect - user.LastConnect;
        if (time != null)
        {
            DailySteamUserInfo todayinfo = GetTodayInfo(user, date);
            todayinfo.OnlineTime += time.Value.TotalMinutes;
        }

        Console.WriteLine($"OFFLINE: {user.CurrentName} ({user.SteamUid}) {user.LastDisconnect}");
    }

    private void Clear()
    {
        foreach (var user in UserConfig.SteamUsers.Where(k => k.IsOnline))
        {
            UpdateDisconnectInfo(user);
            SaveConfig();
        }
    }

    private void SaveConfig()
    {
        File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(UserConfig, Formatting.Indented));
        Console.WriteLine("Config saved.");
    }

    private string ConvertJsonString(string str)
    {
        //格式化json字符串
        JsonSerializer serializer = new JsonSerializer();
        TextReader tr = new StringReader(str);
        JsonTextReader jtr = new JsonTextReader(tr);
        object obj = serializer.Deserialize(jtr);
        if (obj != null)
        {
            StringWriter textWriter = new StringWriter();
            JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
            {
                Formatting = Formatting.Indented,
                Indentation = 4,
                IndentChar = ' '
            };
            serializer.Serialize(jsonWriter, obj);
            return textWriter.ToString();
        }
        else
        {
            return str;
        }
    }
}