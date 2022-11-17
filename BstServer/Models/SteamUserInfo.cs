using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BstServer.Models;

public class DailySteamUserInfo : SteamUserInfo
{
    public DailySteamUserInfo(CustomDate customDate) : base(customDate)
    {
    }
}

public class MonthlySteamUserInfo : SteamUserInfoQuery
{
    private readonly IReadOnlyList<DailySteamUserInfo> _dailyInfos;

    public MonthlySteamUserInfo(IReadOnlyList<DailySteamUserInfo> dailyInfos)
        : base(new CustomDate(dailyInfos.First().CustomDate.Year, dailyInfos.First().CustomDate.Month, 0))
    {
        _dailyInfos = dailyInfos;
        var m = dailyInfos.First().CustomDate.Month;
        var y = dailyInfos.First().CustomDate.Year;
        if (dailyInfos.Skip(1).Any(info =>
                info.CustomDate.Month != m || info.CustomDate.Year != y))
        {
            throw new ArgumentException();
        }

        OnlineTime = dailyInfos.Sum(k => k.OnlineTime);
        SupportedYuan = dailyInfos.Sum(k => k.SupportedYuan);

        WeaponList = SteamUser.GetWeaponInfo(dailyInfos);
    }
}

public class YearlySteamUserInfo : SteamUserInfoQuery
{
    private readonly IReadOnlyList<SteamUserInfo> _monthlyInfos;
    public bool IsCellDaily => _monthlyInfos is IReadOnlyList<DailySteamUserInfo>;

    public YearlySteamUserInfo(IReadOnlyList<SteamUserInfo> infos)
        : base(new CustomDate(infos.First().CustomDate.Year, 0, 0))
    {
        _monthlyInfos = infos;

        var y = infos.First().CustomDate.Year;
        if (infos.Skip(1).Any(info => info.CustomDate.Year != y))
        {
            throw new ArgumentException();
        }

        OnlineTime = infos.Sum(k => k.OnlineTime);
        SupportedYuan = infos.Sum(k => k.SupportedYuan);

        WeaponList = SteamUser.GetWeaponInfo(infos);
    }
}

public abstract class SteamUserInfoQuery : SteamUserInfo
{
    protected SteamUserInfoQuery(CustomDate customDate) : base(customDate)
    {
    }

    public new double OnlineTime { get; protected set; }
    public new decimal SupportedYuan { get; protected set; }

    public new IReadOnlyList<WeaponCell> WeaponList { get; protected set; }
}

public abstract class SteamUserInfo : INotifyPropertyChanged
{
    private decimal _supportedYuan;
    private double _onlineTime;

    public SteamUserInfo(CustomDate customDate)
    {
        CustomDate = customDate;
    }

    public string DateString { get; set; }

    [JsonIgnore]
    public CustomDate CustomDate
    {
        get => CustomDate.Parse(DateString);
        set => DateString = value.ToString();
    }

    public decimal SupportedYuan
    {
        get => _supportedYuan;
        set
        {
            if (_supportedYuan == value) return;
            _supportedYuan = value;
            OnPropertyChanged();
        }
    }

    public double OnlineTime
    {
        get => _onlineTime;
        set
        {
            if (_onlineTime == value) return;
            _onlineTime = value;
            OnPropertyChanged();
        }
    }

    public List<WeaponCell> WeaponInfos { get; set; } = new List<WeaponCell>();

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class WeaponCell
{
    public WeaponCell()
    {

    }
    public WeaponCell(string weaponName)
    {
        WeaponName = weaponName;
    }

    public WeaponCell(string weaponName, int damage, int damageTimes, int hurt, int hurtTimes)
    {
        WeaponName = weaponName;
        Damage = damage;
        DamageTimes = damageTimes;
        Hurt = hurt;
        HurtTimes = hurtTimes;
    }

    public string WeaponName { get; set; }
    public int Damage { get; set; }
    public int DamageTimes { get; set; }
    public int Hurt { get; set; }
    public int HurtTimes { get; set; }
}

public class FfComparer : IComparer<WeaponDetail>
{
    public int Compare(WeaponDetail x, WeaponDetail y)
    {
        int result = 0;
        if (x.Damage > y.Damage) result = 1;
        if (x.Damage < y.Damage) result = -1;
        if (x.Damage == y.Damage)
        {
            if (x.Times > y.Times) result = 1;
            if (x.Times < y.Times) result = -1;
        }

        return -result;
    }
}
public class WeaponInfo
{
    public WeaponInfo(string weapon, string picName)
    {
        Weapon = weapon;
        PicName = picName;
    }

    public void AddDetail(WeaponDetail weaponDetail)
    {
        Detail.Add(weaponDetail);
    }

    public string Weapon { get; }
    public string PicName { get; set; }

    public List<WeaponDetail> Detail { get; } = new List<WeaponDetail>();
}
public struct WeaponDetail
{
    public WeaponDetail(string user, int damage, int times) : this()
    {
        User = user;
        Damage = damage;
        Times = times;
    }

    public string User { get; set; }
    public int Damage { get; set; }
    public int Times { get; set; }
}