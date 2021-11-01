using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Files.Filesystem.Search
{
    public struct Date : IEquatable<Date>, IComparable<Date>, IFormattable
    {
        private readonly DateTime date;

        public static readonly Date MinValue = new(1600, 1, 1);
        public static readonly Date MaxValue = new(9999, 12, 31);

        public static Date Today => new(DateTime.Today);

        public ushort Year => (ushort)date.Year;
        public ushort Month => (ushort)date.Month;
        public ushort Day => (ushort)date.Day;

        public DateTime DateTime => date;
        public DateTimeOffset Offset => new(date);

        public Date(ushort year, ushort month, ushort day)
            : this(new DateTime(year, month, day)) {}
        public Date(DateTime date) => this.date = date.Year switch
        {
            < 1600 => MinValue.date,
            > 9999 => MaxValue.date,
            _ => date.Date,
        };

        public static bool operator ==(Date d1, Date d2) => d1.date == d2.date;
        public static bool operator !=(Date d1, Date d2) => d1.date != d2.date;
        public static bool operator <(Date d1, Date d2) => d1.date < d2.date;
        public static bool operator >(Date d1, Date d2) => d1.date > d2.date;
        public static bool operator <=(Date d1, Date d2) => d1.date <= d2.date;
        public static bool operator >=(Date d1, Date d2) => d1.date >= d2.date;

        public override int GetHashCode() => date.GetHashCode();
        public override bool Equals(object other) => other is Date date && Equals(date);
        public bool Equals(Date other) => other.date.Equals(date);
        public int CompareTo(Date other) => other.date.CompareTo(date);

        public override string ToString() => date.ToString("d");
        public string ToString(string format) => date.ToString(format, CultureInfo.CurrentCulture);
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format is null || format == "G")
            {
                return date.ToString("d", formatProvider);
            }
            return date.ToString(format, formatProvider);
        }

        public Date AddDays(int days) => new(date.AddDays(days));
        public Date AddMonths(int months) => new(date.AddMonths(months));
        public Date AddYears(int years) => new(date.AddYears(years));
    }

    public struct DateRange : IEquatable<DateRange>, IFormattable
    {
        public static event EventHandler TodayUpdated;

        private static DateRange always;
        private static DateRange today;
        private static DateRange yesterday;
        private static DateRange thisWeek;
        private static DateRange lastWeek;
        private static DateRange thisMonth;
        private static DateRange lastMonth;
        private static DateRange thisYear;
        private static DateRange older;

        public static DateRange None => new(false);

        public static DateRange Always => always;
        public static DateRange Today => today;
        public static DateRange Yesterday => yesterday;
        public static DateRange ThisWeek => thisWeek;
        public static DateRange LastWeek => lastWeek;
        public static DateRange ThisMonth => thisMonth;
        public static DateRange LastMonth => lastMonth;
        public static DateRange ThisYear => thisYear;
        public static DateRange Older => older;

        public bool IsNamed => GetIsNamed();

        public Date MinDate { get; }
        public Date MaxDate { get; }

        static DateRange() => UpdateToday();

        public DateRange(Date minDate, Date maxDate)
            => (MinDate, MaxDate) = (minDate <= maxDate) ? (minDate, maxDate) : (maxDate, minDate);
        public DateRange(Date minDate, DateRange maxRange)
            : this(Min(minDate, maxRange.MinDate), Max(minDate, maxRange.MaxDate)) {}
        public DateRange(DateRange minRange, Date maxDate)
            : this(Min(minRange.MinDate, maxDate), Max(minRange.MaxDate, maxDate)) {}
        public DateRange(DateRange minRange, DateRange maxRange)
            : this(Min(minRange.MinDate, maxRange.MinDate), Max(minRange.MaxDate, maxRange.MaxDate)) {}
        private DateRange(bool _) => (MinDate, MaxDate) = (Date.MaxValue, Date.MinValue);

        public void Deconstruct(out Date minDate, out Date maxDate)
            => (minDate, maxDate) = (MinDate, MaxDate);
        public void Deconstruct(out bool isNamed, out Date minDate, out Date maxDate)
            => (isNamed, minDate, maxDate) = (IsNamed, MinDate, MaxDate);

        public static void UpdateToday()
        {
            var date = Date.Today;

            always = new(Date.MinValue, date);
            today = new(date, date);
            yesterday = new(date.AddDays(-1), date.AddDays(-1));
            thisWeek = new(date.AddDays(-6), date.AddDays(-2));
            lastWeek = new(date.AddDays(-13), date.AddDays(-7));
            thisMonth = new(date.AddMonths(-1).AddDays(1), date.AddDays(-14));
            lastMonth = new(date.AddMonths(-2).AddDays(1), date.AddMonths(-1));
            thisYear = new(date.AddYears(-1).AddDays(1), date.AddMonths(-2));
            older = new(Date.MinValue, date.AddYears(-1));

            TodayUpdated?.Invoke(null, EventArgs.Empty);
        }

        public override int GetHashCode()
            => (MinDate, MaxDate).GetHashCode();
        public override bool Equals(object other)
            => other is DateRange range && Equals(range);
        public bool Equals(DateRange other)
            => other is DateRange range && range.MinDate == MinDate && range.MaxDate == MaxDate;

        public override string ToString() => ToString("G");
        public string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (Equals(None) || Equals(Always))
            {
                return string.Empty;
            }

            if (format == "g")
            {
                return ToString("n", formatProvider);
            }
            if (format is null || format == "G")
            {
                return ToString("N", formatProvider);
            }

            var (isNamed, minDate, maxDate) = this;
            bool useName = isNamed && format.ToLower() == "n";

            bool hasMin = minDate > Date.MinValue;
            bool hasMax = maxDate < Date.Today;

            string minLabel = GetMinLabel();
            string maxLabel = GetMaxLabel();

            return format switch
            {
                "n" => string.Format(GetShortFormat(), minLabel, maxLabel),
                "N" => string.Format(GetFullFormat(), minLabel, maxLabel),
                "r" => string.Format(GetShortFormat(), minLabel, maxLabel),
                "R" => string.Format(GetFullFormat(), minLabel, maxLabel),
                "q" => string.Format(GetQueryFormat(), minDate, maxDate),
                "Q" => string.Format(GetQueryFormat(), minDate, maxDate),
                _ => string.Empty,
            };

            string GetMinLabel() => useName switch
            {
                true when Today.MinDate.Equals(minDate) => "ItemTimeText_Today".GetLocalized(),
                true when Yesterday.MinDate.Equals(minDate) => "ItemTimeText_Yesterday".GetLocalized(),
                true when ThisWeek.MinDate.Equals(minDate) => "ItemTimeText_ThisWeek".GetLocalized(),
                true when LastWeek.MinDate.Equals(minDate) => "ItemTimeText_LastWeek".GetLocalized(),
                true when ThisMonth.MinDate.Equals(minDate) => "ItemTimeText_ThisMonth".GetLocalized(),
                true when LastMonth.MinDate.Equals(minDate) => "ItemTimeText_LastMonth".GetLocalized(),
                true when ThisYear.MinDate.Equals(minDate) => "ItemTimeText_ThisYear".GetLocalized(),
                true when Older.MinDate.Equals(minDate) => "ItemTimeText_Older".GetLocalized(),
                true => string.Empty,
                false => $"{minDate}",
            };
            string GetMaxLabel() => useName switch
            {
                true when Today.MaxDate.Equals(maxDate) => "ItemTimeText_Today".GetLocalized(),
                true when Yesterday.MaxDate.Equals(maxDate) => "ItemTimeText_Yesterday".GetLocalized(),
                true when ThisWeek.MaxDate.Equals(maxDate) => "ItemTimeText_ThisWeek".GetLocalized(),
                true when LastWeek.MaxDate.Equals(maxDate) => "ItemTimeText_LastWeek".GetLocalized(),
                true when ThisMonth.MaxDate.Equals(maxDate) => "ItemTimeText_ThisMonth".GetLocalized(),
                true when LastMonth.MaxDate.Equals(maxDate) => "ItemTimeText_LastMonth".GetLocalized(),
                true when ThisYear.MaxDate.Equals(maxDate) => "ItemTimeText_ThisYear".GetLocalized(),
                true when Older.MaxDate.Equals(maxDate) => "ItemTimeText_Older".GetLocalized(),
                true => string.Empty,
                false => $"{maxDate}",
            };

            string GetShortFormat() => (hasMin, hasMax) switch
            {
                _ when minLabel == maxLabel => "{0}",
                (false, _) => "< {1}",
                (_, false) => "> {0}",
                _ => "{0} - {1}",
            };
            string GetFullFormat() => (hasMin, hasMax) switch
            {
                _ when minLabel == maxLabel => "{0}",
                (false, _) => "SearchDateRange_Before".GetLocalized(),
                (_, false) => "SearchDateRange_After".GetLocalized(),
                _ => "SearchDateRange_Between".GetLocalized(),
            };
            string GetQueryFormat() => (hasMin, hasMax) switch
            {
                _ when minDate == maxDate => "{0::yyyyMMdd}",
                (false, _) => "<{1:yyyyMMdd}",
                (_, false) => ">{0:yyyyMMdd}",
                _ => "{0:yyyyMMdd}..{1:yyyyMMdd}",
            };
        }

        public static DateRange operator +(DateRange a, DateRange b) => new(a, b);
        public static DateRange operator -(DateRange a, DateRange b) => Substract(a, b);
        public static bool operator ==(DateRange a, DateRange b) => a.Equals(b);
        public static bool operator !=(DateRange a, DateRange b) => !a.Equals(b);
        public static bool operator <(DateRange a, DateRange b) => a.MaxDate < b.MinDate;
        public static bool operator >(DateRange a, DateRange b) => a.MaxDate > b.MinDate;
        public static bool operator <=(DateRange a, DateRange b) => a.MaxDate <= b.MinDate;
        public static bool operator >=(DateRange a, DateRange b) => a.MaxDate >= b.MinDate;

        public bool Contains(Date size) => size >= MinDate && size <= MaxDate;
        public bool Contains(DateRange range) => range.MinDate >= MinDate && range.MaxDate <= MaxDate;

        private static Date Min(Date a, Date b) => a <= b ? a : b;
        private static Date Max(Date a, Date b) => a >= b ? a : b;

        private static DateRange Substract(DateRange a, DateRange b)
        {
            if (b.MinDate == a.MinDate && b.MaxDate < a.MaxDate)
            {
                return new(b.MaxDate.AddDays(1), a.MaxDate);
            }
            if (b.MaxDate == a.MaxDate && b.MinDate > a.MinDate)
            {
                return new(a.MinDate, b.MinDate.AddDays(-1));
            }
            return None;
        }

        private bool GetIsNamed()
        {
            var (minDate, maxDate) = this;
            var named = new List<DateRange> { Today, Yesterday, ThisWeek, LastWeek, ThisMonth, LastMonth, ThisYear, Older };
            return named.Any(n => n.MinDate == minDate) && named.Any(n => n.MaxDate == maxDate);
        }
    }
}
