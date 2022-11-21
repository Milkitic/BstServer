namespace BstServer.Models;

public partial class SteamUser
{
    public List<DailySteamUserInfo> QueryInfo()
    {
        return DailyInfos.ToList();
    }

    public List<DailySteamUserInfo> QueryInfo(TimeRange year)
    {
        return InnerQueryInfo(year, null, null);
    }

    public List<DailySteamUserInfo> QueryInfo(TimeRange year, TimeRange month)
    {
        return InnerQueryInfo(year, month, null);
    }

    public List<DailySteamUserInfo> QueryInfo(TimeRange year, TimeRange month, TimeRange day)
    {
        return InnerQueryInfo(year, month, day);
    }

    public DailySteamUserInfo GetDayInfo(DateTime dateTime)
    {
        var list = QueryInfo(dateTime.Year, dateTime.Month, dateTime.Day);
        if (list.Count == 0) return null;
        return list.First();
    }

    public DailySteamUserInfo GetDayInfo(int year, int month, int day)
    {
        var list = QueryInfo(year, month, day);
        if (list.Count == 0) return null;
        return list.First();
    }

    public MonthlySteamUserInfo GetMonthInfo(int year, int month)
    {
        var list = QueryInfo(year, month);
        if (list.Count == 0) return null;
        return new MonthlySteamUserInfo(list);
    }
    public YearlySteamUserInfo GetYearInfo(int year)
    {
        var list = QueryInfo(year);
        if (list.Count == 0) return null;
        return new YearlySteamUserInfo(list);
    }

    public Dictionary<string, DailySteamUserInfo> GetDailyInfo()
    {
        var list = QueryInfo();
        if (list.Count == 0) return new Dictionary<string, DailySteamUserInfo>();
        return list.ToDictionary(k => k.CustomDate.Daily.ToString(), k => k);
    }

    public Dictionary<string, MonthlySteamUserInfo> GetMonthlyInfo()
    {
        var list = QueryInfo();
        if (list.Count == 0) return new Dictionary<string, MonthlySteamUserInfo>();
        return list.GroupBy(k => k.CustomDate.Monthly)
            .ToDictionary(k => k.Key.ToString(), k => new MonthlySteamUserInfo(k.ToList()));
    }

    public Dictionary<string, YearlySteamUserInfo> GetYearlyInfo()
    {
        var list = QueryInfo();
        if (list.Count == 0) return new Dictionary<string, YearlySteamUserInfo>();
        return list.GroupBy(k => k.CustomDate.Yearly)
            .ToDictionary(k => k.Key.ToString(), k => new YearlySteamUserInfo(k.ToList()));
    }

    private List<DailySteamUserInfo> InnerQueryInfo(TimeRange year, TimeRange? month, TimeRange? day)
    {
        year = new TimeRange(year.StartTime % 100, year.EndTime % 100);
        return DailyInfos.Where(k =>
        {
            CustomDate date = k.CustomDate;
            bool flag = date.Year >= year.StartTime && date.Year <= year.EndTime;
            if (!flag) return false;
            if (month != null)
                flag = date.Month >= month.Value.StartTime && date.Month <= month.Value.EndTime;
            if (!flag) return false;
            if (day != null)
                flag = date.Day >= day.Value.StartTime && date.Day <= day.Value.EndTime;
            return flag;
        }).ToList();
    }

    /// <summary>
    /// Get sum for Damage and group by weapons from user information list .
    /// </summary>
    public static List<WeaponCell> GetWeaponInfo(IReadOnlyCollection<SteamUserInfo> steamUserInfos)
    {
        if (steamUserInfos.Count == 0)
            return new List<WeaponCell>();
        return steamUserInfos.Skip(1)
            .Aggregate(steamUserInfos.First().WeaponInfos, (c, info) => c.Concat(info.WeaponInfos).ToList())
            .GroupBy(k => k.WeaponName).Select(k => new WeaponCell(k.Key, k.Sum(cell => cell.Damage),
                k.Sum(cell => cell.DamageTimes), k.Sum(cell => cell.Hurt), k.Sum(cell => cell.HurtTimes))).ToList();
    }
}