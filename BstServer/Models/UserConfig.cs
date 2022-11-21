using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Newtonsoft.Json;

namespace BstServer.Models;

public class UserConfig
{
    private double _cachedTotalRate;
    private ObservableCollection<SteamUser> _steamUsers;

    private object _lockObj = new object();

    public UserConfig()
    {
        SteamUsers = new ObservableCollection<SteamUser>();
        CacheChanged = true;
    }

    public double CachedTotalRate
    {
        get
        {
            lock (_lockObj)
            {
                if (CacheChanged)
                {
                    double totalRate = 0;
                    foreach (var steamUser in SteamUsers)
                    {
                        List<DailySteamUserInfo> sb = steamUser.QueryInfo();

                        double playTime = sb.Sum(k => k.OnlineTime);
                        decimal support = sb.Sum(k => k.SupportedYuan);
                        double rate = (double)support / playTime;
                        totalRate += rate;
                    }

                    _cachedTotalRate = totalRate;

                    CacheChanged = false;
                }
            }

            return _cachedTotalRate;
        }
    }

    [JsonIgnore]
    public bool CacheChanged { get; set; }

    [JsonIgnore]
    public bool ItemChanged { get; set; }

    public string StartTime { get; set; }

    public ObservableCollection<SteamUser> SteamUsers
    {
        get => _steamUsers;
        set
        {
            UnRegisterEvent();
            _steamUsers = value;
            RegisterEvent();
            _steamUsers.CollectionChanged += OnUserCollectionChanged;
        }
    }

    public double GetUserRate(SteamUser user, TimeRange y, TimeRange m, TimeRange d)
    {
        return InnerGetUserRate(user, y, m, d);
    }

    public double GetUserRate(SteamUser user, TimeRange y, TimeRange m)
    {
        return InnerGetUserRate(user, y, m, null);
    }

    public double GetUserRate(SteamUser user, TimeRange y)
    {
        return InnerGetUserRate(user, y, null, null);
    }

    public double GetUserRate(SteamUser user)
    {
        return InnerGetUserRate(user, null, null, null);
    }

    private readonly ConcurrentDictionary<(TimeRange? y, TimeRange? m, TimeRange? d), double> _dicTmpTotal =
        new ConcurrentDictionary<(TimeRange? y, TimeRange? m, TimeRange? d), double>();

    private double InnerGetUserRate(SteamUser user, TimeRange? y, TimeRange? m, TimeRange? d)
    {
        double totalRate = 0, currentRate = 0;
        bool useCache = false;
        if (_dicTmpTotal.ContainsKey((y, m, d)))
        {
            if (ItemChanged)
            {
                _dicTmpTotal.TryRemove((y, m, d), out _);
            }
            else
            {
                useCache = true;
                totalRate = _dicTmpTotal[(y, m, d)];
                if (totalRate == 0) return 0;
            }
        }

        ItemChanged = false;

        foreach (var steamUser in SteamUsers)
        {
            if (user.SteamUid != steamUser.SteamUid && useCache)
                continue;
            List<DailySteamUserInfo> sb;
            if (d == null)
                if (m == null)
                    if (y == null)
                        sb = steamUser.QueryInfo();
                    else
                        sb = steamUser.QueryInfo(y.Value);
                else
                    sb = steamUser.QueryInfo(y.Value, m.Value);
            else
                sb = steamUser.QueryInfo(y.Value, m.Value, d.Value);
            if (sb.Count == 0)
                continue;
            double playTime = sb.Sum(k => k.OnlineTime);
            decimal support = sb.Sum(k => k.SupportedYuan);
            double rate = (double)support / playTime;
            if (user.SteamUid == steamUser.SteamUid)
                currentRate = rate;
            if (!useCache)
                totalRate += rate;
        }

        if (!_dicTmpTotal.ContainsKey((y, m, d)))
        {
            _dicTmpTotal.TryAdd((y, m, d), totalRate);
        }

        if (totalRate == 0) return 0;
        return currentRate / totalRate;
    }

    private List<(SteamUser, double)> InnerGetUserRates(TimeRange? y, TimeRange? m, TimeRange? d)
    {
        return SteamUsers.Select(k => (k, InnerGetUserRate(k, y, m, d))).ToList();
    }

    private void RegisterEvent()
    {
        foreach (var info in _steamUsers)
        {
            info.PropertyChanged += OnUserItemChanged;
        }
    }

    private void UnRegisterEvent()
    {
        if (_steamUsers != null)
        {
            foreach (var info in _steamUsers)
            {
                info.PropertyChanged -= OnUserItemChanged;
            }
        }
    }

    private void OnUserItemChanged(object sender, PropertyChangedEventArgs e)
    {
        lock (_lockObj)
        {
            if (!CacheChanged)
                CacheChanged = true;
            if (!ItemChanged)
                ItemChanged = true;
        }
    }

    private void OnUserCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (SteamUser item in e.NewItems)
            {
                item.PropertyChanged += OnUserItemChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (SteamUser item in e.OldItems)
            {
                item.PropertyChanged -= OnUserItemChanged;
            }
        }

        lock (_lockObj)
        {
            if (!CacheChanged)
                CacheChanged = true;
            if (!ItemChanged)
                ItemChanged = true;
        }
    }
}