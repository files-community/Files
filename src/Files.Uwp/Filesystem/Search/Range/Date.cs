using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Files.Filesystem.Search
{
    public struct Date : IEquatable<Date>, IComparable<Date>, IFormattable
    {
        public static event EventHandler TodayUpdated;

        private const ushort minYear = 1600;
        private const ushort maxYear = 9999;

        private readonly DateTime date;

        public static readonly Date MinValue = new(minYear, 1, 1);
        public static readonly Date MaxValue = new(maxYear, 12, 31);

        private static Date today = new(DateTime.Today);
        public static Date Today => today;

        public ushort Year => (ushort)date.Year;
        public ushort Month => (ushort)date.Month;
        public ushort Day => (ushort)date.Day;

        public DateTime DateTime => date;
        public DateTimeOffset Offset => new(date);

        public Date(ushort year, ushort month, ushort day)
            : this(new DateTime(year, month, day)) {}
        public Date(DateTime date) => this.date = date.Year switch
        {
            < minYear => MinValue.date,
            > maxYear => MaxValue.date,
            _ => date.Date,
        };

        public static bool operator ==(Date d1, Date d2) => d1.date == d2.date;
        public static bool operator !=(Date d1, Date d2) => d1.date != d2.date;
        public static bool operator <(Date d1, Date d2) => d1.date < d2.date;
        public static bool operator >(Date d1, Date d2) => d1.date > d2.date;
        public static bool operator <=(Date d1, Date d2) => d1.date <= d2.date;
        public static bool operator >=(Date d1, Date d2) => d1.date >= d2.date;

        public Date AddDays(int days) => new(date.AddDays(days));
        public Date AddMonths(int months) => new(date.AddMonths(months));
        public Date AddYears(int years) => new(date.AddYears(years));

        public override int GetHashCode() => date.GetHashCode();
        public override bool Equals(object other) => other is Date date && Equals(date);
        public bool Equals(Date other) => other.date.Equals(date);
        public int CompareTo(Date other) => other.date.CompareTo(date);

        public override string ToString() => ToString("d");
        public string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format is null || format == "G")
            {
                return date.ToString("d", formatProvider);
            }
            return date.ToString(format, formatProvider);
        }

        public static void UpdateToday()
        {
            var oldToday = Today;
            var newToday = new Date(DateTime.Today);

            if (oldToday < newToday)
            {
                today = new(DateTime.Today);
                TodayUpdated.Invoke(null, EventArgs.Empty);
            }
        }
    }

    public struct DateRange : IRange<Date>, IEquatable<IRange<Date>>
    {
        private readonly IRange range;

        public static readonly DateRange None = new(new NoneRange());
        public static readonly DateRange Always = new(Date.MinValue, Date.MaxValue);

        public static readonly DateRange Today = new(new RelativeRange(RelativeRange.Moments.Today));
        public static readonly DateRange Yesterday = new(new RelativeRange(RelativeRange.Moments.Yesterday));
        public static readonly DateRange ThisWeek = new(new RelativeRange(RelativeRange.Moments.ThisWeek));
        public static readonly DateRange LastWeek = new(new RelativeRange(RelativeRange.Moments.LastWeek));
        public static readonly DateRange ThisMonth = new(new RelativeRange(RelativeRange.Moments.ThisMonth));
        public static readonly DateRange LastMonth = new(new RelativeRange(RelativeRange.Moments.LastMonth));
        public static readonly DateRange ThisYear = new(new RelativeRange(RelativeRange.Moments.ThisYear));
        public static readonly DateRange Older = new(new RelativeRange(RelativeRange.Moments.Older));

        public bool IsRelative => range is NoneRange || range is AlwaysRange || range is RelativeRange;

        public RangeDirections Direction => range.Direction;

        public Date MinValue => range.MinValue;
        public Date MaxValue => range.MaxValue;

        public IRange<string> Label => range.Label;

        public DateRange(Date minDate, Date maxDate) => range = GetRange(minDate, maxDate);
        private DateRange(IRange range) => this.range = range;

        public void Deconstruct(out Date minValue, out Date maxValue)
            => (_, minValue, maxValue) = range;
        public void Deconstruct(out RangeDirections direction, out Date minValue, out Date maxValue)
            => (direction, minValue, maxValue) = range;

        public override string ToString() => Label.ToString();

        public override int GetHashCode() => range.GetHashCode();
        public override bool Equals(object other) => other is DateRange r && range.Equals(r.range);
        public bool Equals(IRange<Date> other) => other is DateRange r && range.Equals(r.range);

        public static DateRange operator +(DateRange a, DateRange b)
        {
            var (minDateA, maxDateA) = a;
            var (minDateB, maxDateB) = b;

            Date minDate = minDateA <= minDateB ? minDateA : minDateB;
            Date maxDate = maxDateB >= maxDateA ? maxDateB : maxDateA;

            return new(minDate, maxDate);
        }
        public static DateRange operator -(DateRange a, DateRange b)
        {
            var (minDateA, maxDateA) = a;
            var (minDateB, maxDateB) = b;

            if (minDateB == minDateA && maxDateB < maxDateA)
            {
                return new(maxDateB.AddDays(1), maxDateA);
            }
            if (maxDateB == maxDateA && minDateB > minDateA)
            {
                return new(minDateA, minDateB.AddDays(-1));
            }
            return None;
        }
        public static bool operator ==(DateRange a, DateRange b) => a.Equals(b);
        public static bool operator !=(DateRange a, DateRange b) => !a.Equals(b);
        public static bool operator <(DateRange a, DateRange b) => a.MaxValue < b.MinValue;
        public static bool operator >(DateRange a, DateRange b) => a.MaxValue > b.MinValue;
        public static bool operator <=(DateRange a, DateRange b) => a.MaxValue <= b.MinValue;
        public static bool operator >=(DateRange a, DateRange b) => a.MaxValue >= b.MinValue;

        public bool Contains(Date date)
        {
            var (minValue, maxValue) = this;
            return minValue <= date && date <= maxValue;
        }
        public bool Contains(DateRange range)
        {
            var (minValueThis, maxValueThis) = this;
            var (minValueRange, maxValueRange) = range;
            return minValueThis <= minValueRange && maxValueRange <= maxValueThis;
        }

        private static IRange GetRange(Date minDate, Date maxDate)
        {
            Date today = Date.Today;

            if (minDate > maxDate)
            {
                (minDate, maxDate) = (maxDate, minDate);
            }
            if (minDate > today)
            {
                minDate = today;
            }
            if (maxDate > today)
            {
                maxDate = today;
            }

            if (minDate == Date.MinValue && maxDate == today)
            {
                return new AlwaysRange();
            }

            var momentDates = RelativeRange.MomentDates.ToList();

            bool hasMinMoment = momentDates.Any(momentDate => momentDate.minDate == minDate);
            bool hasMaxMoment = momentDates.Any(momentDate => momentDate.maxDate == maxDate);

            if (!hasMinMoment || !hasMaxMoment)
            {
                return new CustomRange(minDate, maxDate);
            }

            RelativeRange.Moments minMoment = momentDates.First(momentDate => momentDate.minDate == minDate).moment;
            RelativeRange.Moments maxMoment = momentDates.First(momentDate => momentDate.maxDate == maxDate).moment;

            return new RelativeRange(minMoment, maxMoment);
        }

        private interface IRange : IRange<Date>, IEquatable<IRange<Date>>
        {
            void Deconstruct(out RangeDirections direction, out Date minValue, out Date maxValue);
        }

        private struct NoneRange : IRange
        {
            public RangeDirections Direction => RangeDirections.None;

            public Date MinValue => Date.MaxValue;
            public Date MaxValue => Date.MinValue;

            public IRange<string> Label => RangeLabel.None;

            public void Deconstruct(out RangeDirections direction, out Date minValue, out Date maxValue)
                => (direction, minValue, maxValue) = (Direction, MinValue, MaxValue);

            public override int GetHashCode() => Direction.GetHashCode();
            public override bool Equals(object other) => other is NoneRange;
            public bool Equals(IRange<Date> other) => other is NoneRange;
        }

        private struct AlwaysRange : IRange
        {
            public RangeDirections Direction => RangeDirections.All;

            public Date MinValue => Date.MinValue;
            public Date MaxValue => Date.Today;

            public IRange<string> Label => RangeLabel.None;

            public void Deconstruct(out RangeDirections direction, out Date minValue, out Date maxValue)
                => (direction, minValue, maxValue) = (Direction, MinValue, MaxValue);

            public override int GetHashCode() => Direction.GetHashCode();
            public override bool Equals(object other) => other is AlwaysRange;
            public bool Equals(IRange<Date> other) => other is AlwaysRange;
        }

        private struct RelativeRange : IRange
        {
            public enum Moments : ushort { Today, Yesterday, ThisWeek, LastWeek, ThisMonth, LastMonth, ThisYear, Older }

            public static IEnumerable<(Moments moment, Date minDate, Date maxDate)> MomentDates
            {
                get
                {
                    Date today = Date.Today;

                    return Enum.GetValues(typeof(Moments)).Cast<Moments>()
                        .Select(moment => (moment, GetMinDate(today, moment), GetMaxDate(today, moment)));
                }
            }

            private readonly Moments minMoment;
            private readonly Moments maxMoment;

            public RangeDirections Direction => (minMoment, maxMoment) switch
            {
                _ when minMoment == maxMoment => RangeDirections.EqualTo,
                (Moments.Older, _) => RangeDirections.LessThan,
                (_, Moments.Today) => RangeDirections.GreaterThan,
                _ => RangeDirections.Between,
            };

            public Date MinValue => GetMinDate(Date.Today, minMoment);
            public Date MaxValue => GetMaxDate(Date.Today, maxMoment);

            public IRange<string> Label
            {
                get
                {
                    if (minMoment == maxMoment)
                    {
                        return new RangeLabel(GetText(minMoment));
                    }

                    string minLabel = minMoment != Moments.Older ? GetText(minMoment) : string.Empty;
                    string maxLabel = maxMoment != Moments.Today ? GetText(maxMoment) : string.Empty;

                    return new RangeLabel(minLabel, maxLabel);
                }
            }

            public RelativeRange(Moments moment)
                => minMoment = maxMoment = moment;
            public RelativeRange(Moments minMoment, Moments maxMoment)
                => (this.minMoment, this.maxMoment) = (minMoment, maxMoment);

            public void Deconstruct(out RangeDirections direction, out Date minValue, out Date maxValue)
            {
                Date today = Date.Today;
                direction = Direction;
                minValue = GetMinDate(today, minMoment);
                maxValue = GetMaxDate(today, maxMoment);
            }

            public override int GetHashCode()
                => (minMoment, maxMoment).GetHashCode();
            public override bool Equals(object other)
                => other is RelativeRange range && Equals(range);
            public bool Equals(IRange<Date> other)
                => other is RelativeRange range && range.minMoment == minMoment && range.maxMoment == maxMoment;

            private static Date GetMinDate(Date today, Moments moment) => moment switch
            {
                Moments.Today => today,
                Moments.Yesterday => today.AddDays(-1),
                Moments.ThisWeek => today.AddDays(-6),
                Moments.LastWeek => today.AddDays(-13),
                Moments.ThisMonth => today.AddMonths(-1).AddDays(1),
                Moments.LastMonth => today.AddMonths(-2).AddDays(1),
                Moments.ThisYear => today.AddYears(-1).AddDays(1),
                Moments.Older => Date.MinValue,
                _ => throw new ArgumentException(),
            };
            private static Date GetMaxDate(Date today, Moments moment) => moment switch
            {
                Moments.Today => today,
                Moments.Yesterday => today.AddDays(-1),
                Moments.ThisWeek => today.AddDays(-2),
                Moments.LastWeek => today.AddDays(-7),
                Moments.ThisMonth => today.AddDays(-14),
                Moments.LastMonth => today.AddMonths(-1),
                Moments.ThisYear => today.AddMonths(-2),
                Moments.Older => today.AddYears(-1),
                _ => throw new ArgumentException(),
            };
            private static string GetText(Moments moment) => moment switch
            {
                Moments.Today => "Today".GetLocalized(),
                Moments.Yesterday => "ItemTimeText_Yesterday".GetLocalized(),
                Moments.ThisWeek => "ItemTimeText_ThisWeek".GetLocalized(),
                Moments.LastWeek => "ItemTimeText_LastWeek".GetLocalized(),
                Moments.ThisMonth => "ItemTimeText_ThisMonth".GetLocalized(),
                Moments.LastMonth => "ItemTimeText_LastMonth".GetLocalized(),
                Moments.ThisYear => "ItemTimeText_ThisYear".GetLocalized(),
                Moments.Older => "ItemTimeText_Older".GetLocalized(),
                _ => throw new ArgumentException(),
            };
        }

        public struct CustomRange : IRange
        {
            private static readonly ILabelBuilder labelBuilder = new LabelBuilderCollection
            {
                new YearBuilder(),
                new MonthBuilder(),
                new DayWeekBuilder(),
                new DayMonthBuilder(),
                new DayBuilder(),
            };

            public RangeDirections Direction
            {
                get
                {
                    bool hasMin = MinValue > Date.MinValue;
                    bool hasMax = MaxValue < Date.Today;

                    return (hasMin, hasMax) switch
                    {
                        (false, false) => RangeDirections.None,
                        _ when MinValue == MaxValue => RangeDirections.EqualTo,
                        (true, false) => RangeDirections.GreaterThan,
                        (false, true) => RangeDirections.LessThan,
                        _ => RangeDirections.Between,
                    };
                }
            }

            public Date MinValue { get; }
            public Date MaxValue { get; }

            public IRange<string> Label
            {
                get
                {
                    Date? minDate = MinValue > Date.MinValue ? MinValue : null;
                    Date? maxDate = MaxValue < Date.Today ? MaxValue : null;

                    if (labelBuilder.CanBuild(minDate, maxDate))
                    {
                        return labelBuilder.Build(minDate, maxDate);
                    }
                    return RangeLabel.None;
                }
            }

            public CustomRange(Date minDate, Date maxDate)
            {
                MinValue = minDate <= maxDate ? minDate : maxDate;
                MaxValue = maxDate >= minDate ? maxDate : minDate;
            }

            public void Deconstruct(out RangeDirections direction, out Date minValue, out Date maxValue)
                => (direction, minValue, maxValue) = (Direction, MinValue, MaxValue);

            public override int GetHashCode()
                => (MinValue, MaxValue).GetHashCode();
            public override bool Equals(object other)
                => other is CustomRange range && Equals(range);
            public bool Equals(IRange<Date> other)
                => other is CustomRange range && range.MinValue == MinValue && range.MaxValue == MaxValue;

            private interface ILabelBuilder
            {
                bool CanBuild(Date? minDate, Date? maxDate);
                IRange<string> Build(Date? minDate, Date? maxDate);
            }

            private class LabelBuilderCollection : Collection<ILabelBuilder>, ILabelBuilder
            {
                public LabelBuilderCollection() : base() {}
                public LabelBuilderCollection(IList<ILabelBuilder> builders) : base(builders) {}

                public bool CanBuild(Date? minDate, Date? maxDate)
                    => this.Any(builder => builder.CanBuild(minDate, maxDate));
                public IRange<string> Build(Date? minDate, Date? maxDate)
                    => this.First(builder => builder.CanBuild(minDate, maxDate)).Build(minDate, maxDate);
            }

            private class YearBuilder : ILabelBuilder
            {
                public bool CanBuild(Date? minDate, Date? maxDate)
                {
                    bool hasMin = !minDate.HasValue || (minDate.Value.Month == 1 && minDate.Value.Day == 1);
                    bool hasMax = !maxDate.HasValue || (maxDate.Value.Month == 12 && maxDate.Value.Day == 31);

                    return hasMin && hasMax;
                }
                public IRange<string> Build(Date? minDate, Date? maxDate)
                {
                    if (minDate.HasValue)
                    {
                        Date min = minDate.Value;
                        Date today = Date.Today;
                        if (min.Year == today.Year)
                        {
                            return GetRangeLabel("yyyy", today, today);
                        }
                    }
                    return GetRangeLabel("yyyy", minDate, maxDate ?? Date.Today);
                }
            }

            private class MonthBuilder : ILabelBuilder
            {
                public bool CanBuild(Date? minDate, Date? maxDate)
                {
                    bool hasMin = !minDate.HasValue || minDate.Value.Day == 1;
                    bool hasMax = !maxDate.HasValue || maxDate.Value.AddDays(1).Day == 1;

                    return hasMin && hasMax;
                }
                public IRange<string> Build(Date? minDate, Date? maxDate)
                {
                    if (minDate.HasValue)
                    {
                        Date min = minDate.Value;
                        Date today = Date.Today;
                        if (min.Year == today.Year && min.Month == today.Month)
                        {
                            return GetRangeLabel("Y", today, today);
                        }
                    }
                    return GetRangeLabel("Y", minDate, maxDate);
                }
            }

            private class DayWeekBuilder : ILabelBuilder
            {
                public bool CanBuild(Date? minDate, Date? maxDate)
                {
                    Date MonthAgo = Date.Today.AddMonths(-1);

                    bool hasMin = !minDate.HasValue || minDate.Value > MonthAgo;
                    bool hasMax = !maxDate.HasValue || maxDate.Value > MonthAgo;

                    return hasMin && hasMax;
                }
                public IRange<string> Build(Date? minDate, Date? maxDate) => GetRangeLabel("dddd d", minDate, maxDate);
            }

            private class DayMonthBuilder : ILabelBuilder
            {
                public bool CanBuild(Date? minDate, Date? maxDate)
                {
                    Date YearAgo = Date.Today.AddYears(-1);

                    bool hasMin = !minDate.HasValue || minDate.Value > YearAgo;
                    bool hasMax = !maxDate.HasValue || maxDate.Value > YearAgo;

                    return hasMin && hasMax;
                }
                public IRange<string> Build(Date? minDate, Date? maxDate) => GetRangeLabel("M", minDate, maxDate);
            }

            private class DayBuilder : ILabelBuilder
            {
                public bool CanBuild(Date? minDate, Date? maxDate) => true;
                public IRange<string> Build(Date? minDate, Date? maxDate) => GetRangeLabel("G", minDate, maxDate);
            }

            private static IRange<string> GetRangeLabel(string format, Date? minDate, Date? maxDate)
            {
                Date yesterday = Date.Today.AddDays(-1);
                return new RangeLabel(GetText(minDate), GetText(maxDate));

                string GetText(Date? date) => date.HasValue
                    ? GetDateText(date.Value) : string.Empty;
                string GetDateText(Date date) => date == yesterday
                    ? "ItemTimeText_Yesterday".GetLocalized() : date.ToString(format);
            }
        }
    }
}
