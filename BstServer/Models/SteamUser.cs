using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BstServer.Models;

public partial class SteamUser : INotifyPropertyChanged
{
    private ObservableCollection<DailySteamUserInfo> _dailyInfos;

    public SteamUser()
    {
        DailyInfos = new ObservableCollection<DailySteamUserInfo>();
    }

    [DisplayName("Steam Uid")]
    public string SteamUid { get; set; }

    [DisplayName("当前用户名")]
    public string CurrentName { get; set; }

    [JsonIgnore]
    [DisplayName("当前状态")]

    public bool IsOnline { get; set; } = false;

    [JsonIgnore]
    [DisplayName("总计捐赠（元）")]
    public decimal TotalSupportedYuan => DailyInfos.Select(k => k.SupportedYuan).Sum();

    [JsonIgnore]
    [DisplayName("总计在线时间")]
    public double TotalOnlineTime => DailyInfos.Select(k => k.OnlineTime).Sum();

    [DisplayName("上次连接时间")]
    public DateTime? LastConnect { get; set; }

    [DisplayName("上次断开时间")]
    public DateTime? LastDisconnect { get; set; }

    public ObservableCollection<DailySteamUserInfo> DailyInfos
    {
        get => _dailyInfos;
        set
        {
            UnRegisterEvent();
            _dailyInfos = value;
            RegisterEvent();
            _dailyInfos.CollectionChanged += OnInfoCollectionChanged;
        }
    }

    [JsonIgnore]
    [DisplayName("总计黑枪")]
    public List<(string, int)> TotalDamageInfo =>
        GetWeaponInfo(DailyInfos).Select(k => (k.WeaponName, k.Damage)).ToList();

    [JsonIgnore]
    [DisplayName("总计被黑")]
    public List<(string, int)> TotalHurtInfo =>
        GetWeaponInfo(DailyInfos).Select(k => (k.WeaponName, k.Hurt)).ToList();

    [JsonIgnore]
    [DisplayName("总计黑枪次数")]
    public List<(string, int)> TotalDamageTimesInfo =>
        GetWeaponInfo(DailyInfos).Select(k => (k.WeaponName, k.DamageTimes)).ToList();

    [JsonIgnore]
    [DisplayName("总计被黑次数")]
    public List<(string, int)> TotalHurtTimesInfo =>
        GetWeaponInfo(DailyInfos).Select(k => (k.WeaponName, k.HurtTimes)).ToList();

    public List<(string name, int damage, int damageTimes, int hurt, int hurtTimes)> TotalWeaponInfo =>
        GetWeaponInfo(DailyInfos).Select(k => (k.WeaponName, k.Damage, k.DamageTimes, k.Hurt, k.HurtTimes)).ToList();

    public event PropertyChangedEventHandler PropertyChanged;

    //protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    //{
    //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //}

    private void OnInfoItemChanged(object sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(sender, e);
    }

    private void OnInfoCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (DailySteamUserInfo item in e.NewItems)
            {
                item.PropertyChanged += OnInfoItemChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (DailySteamUserInfo item in e.OldItems)
            {
                item.PropertyChanged -= OnInfoItemChanged;
            }
        }
    }

    private void RegisterEvent()
    {
        foreach (var info in _dailyInfos)
        {
            info.PropertyChanged += OnInfoItemChanged;
        }
    }

    private void UnRegisterEvent()
    {
        if (_dailyInfos != null)
        {
            foreach (var info in _dailyInfos)
            {
                info.PropertyChanged -= OnInfoItemChanged;
            }
        }
    }
}