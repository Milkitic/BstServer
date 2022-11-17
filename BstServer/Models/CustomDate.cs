using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BstServer.Models;

public struct CustomDate
{
    private int _year;
    private int _month;
    private int _day;

    public CustomDate(DateTime dateTime)
        : this(dateTime.Year, dateTime.Month, dateTime.Day)
    {
    }

    public CustomDate(int year, int month, int day)
    {
        _year = year % 100;
        _month = month;
        _day = day;
    }

    public int Year
    {
        get => _year;
        set
        {
            if (value < 1900)
            {
                throw new ArgumentException("Invalid year.");
            }
            _year = value;
        }
    }

    public int Month
    {
        get => _month;
        set
        {
            if (value > 12 || value < 0)
            {
                throw new ArgumentException("Invalid month.");
            }
            _month = value;
        }
    }

    public int Day
    {
        get => _day;
        set
        {
            if (value > 31 || value < 0)
            {
                throw new ArgumentException("Invalid day.");
            }
            _day = value;
        }
    }
    public CustomDate Yearly => new CustomDate
    {
        Year = this.Year,
        Month = 0,
        Day = 0
    };
    public CustomDate Monthly => new CustomDate
    {
        Year = this.Year,
        Month = this.Month,
        Day = 0
    };

    public CustomDate Daily => this;

    public static bool operator ==(CustomDate d1, CustomDate d2)
    {
        return d1.ToString() == d2.ToString();
    }

    public static bool operator !=(CustomDate d1, CustomDate d2)
    {
        return !(d1 == d2);
    }

    public bool Equals(CustomDate other)
    {
        return Year == other.Year && Month == other.Month && Day == other.Day;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is CustomDate date && Equals(date);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Year;
            hashCode = (hashCode * 397) ^ Month;
            hashCode = (hashCode * 397) ^ Day;
            return hashCode;
        }
    }

    public override string ToString()
    {
        string y = Year.ToString();
        string m = Month.ToString("00");
        string d = Day.ToString("00");

        return (y.Length == 4 ? y.Substring(2) : y) + m + d;
    }

    public static CustomDate Parse(string customDate)
    {
        if (customDate.Length != 6)
            throw new ArgumentException("Length should be 6. E.g.: 150920");
        string[] array = customDate.SplitByStep(2);
        int y = int.Parse(array[0]);
        int m = int.Parse(array[1]);
        int d = int.Parse(array[2]);
        return new CustomDate
        {
            _day = d,
            _month = m,
            _year = y
        };
    }
}