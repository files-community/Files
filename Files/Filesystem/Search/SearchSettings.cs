using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Files.Filesystem.Search
{
    public interface ISettings
    {
        ILocation Location { get; }
        IFilter Filter { get; }
    }

    public interface ILocation : INotifyPropertyChanged
    {
        ObservableCollection<string> Folders { get; }
        LocationOptions Options { get; set; }
    }

    [Flags]
    public enum LocationOptions : ushort
    {
        None = 0x0000,
        SubFolders = 0x0001,
        SystemFiles = 0x0002,
        CompressedFiles = 0x0004,
    }

    public interface IFilter
    {
        string ToAdvancedQuerySyntax();
    }

    public class Settings : ObservableObject, ISettings
    {
        public static Settings Default { get; } = new();

        public ILocation Location { get; } = new Location();
        public IFilter Filter { get; } = new AndFilterCollection();

        private Settings() {}
    }

    public class Location : ObservableObject, ILocation
    {
        public ObservableCollection<string> Folders { get; } = new ObservableCollection<string>();

        private LocationOptions options = LocationOptions.SubFolders;
        public LocationOptions Options
        {
            get => options;
            set => SetProperty(ref options, value);
        }
    }

    public class FilterCollection : ObservableCollection<IFilter>, IFilter
    {
        public FilterCollection() : base() {}
        public FilterCollection(IEnumerable<IFilter> filters) : base(filters) {}
        public FilterCollection(IList<IFilter> filters) : base(filters) {}

        public virtual string ToAdvancedQuerySyntax() => string.Empty;
    }
    public class AndFilterCollection : FilterCollection
    {
        public AndFilterCollection() : base() {}
        public AndFilterCollection(IEnumerable<IFilter> filters) : base(filters) {}
        public AndFilterCollection(IList<IFilter> filters) : base(filters) {}

        public override string ToAdvancedQuerySyntax()
        {
            var queries = this
                .Where(filter => filter is not null)
                .Select(filter => (filter.ToAdvancedQuerySyntax() ?? string.Empty).Trim())
                .Where(query => !string.IsNullOrEmpty(query))
                .Select(query => query.Contains(' ') ? $"({query})" : query);
            return string.Join(' ', queries);
        }
    }
    public class OrFilterCollection : FilterCollection
    {
        public OrFilterCollection() : base() {}
        public OrFilterCollection(IEnumerable<IFilter> filters) : base(filters) {}
        public OrFilterCollection(IList<IFilter> filters) : base(filters) {}

        public override string ToAdvancedQuerySyntax()
        {
            var queries = this
                .Where(filter => filter is not null)
                .Select(filter => (filter.ToAdvancedQuerySyntax() ?? string.Empty).Trim())
                .Where(query => !string.IsNullOrEmpty(query))
                .Select(query => query.Contains(' ') ? $"({query})" : query);
            return string.Join(" or ", queries);
        }
    }
    public class NotFilterCollection : FilterCollection
    {
        public NotFilterCollection() : base() {}
        public NotFilterCollection(IEnumerable<IFilter> filters) : base(filters) {}
        public NotFilterCollection(IList<IFilter> filters) : base(filters) {}

        public override string ToAdvancedQuerySyntax()
        {
            var queries = this
                .Where(filter => filter is not null)
                .Select(filter => (filter.ToAdvancedQuerySyntax() ?? string.Empty).Trim())
                .Where(query => !string.IsNullOrEmpty(query))
                .Select(query => $"not({query})");
            return string.Join(' ', queries);
        }
    }

    public abstract class DateRangeFilter : IFilter
    {
        public DateRange Range { get; }

        protected abstract string QueryKey { get; }

        public DateRangeFilter() => Range = DateRange.Always;
        public DateRangeFilter(DateRange range) => Range = range;

        public string ToAdvancedQuerySyntax()
        {
            var (min, max) = Range;
            bool hasMin = min > Date.MinValue;
            bool hasMax = max < Date.Today;

            return (hasMin, hasMax) switch
            {
                (false, false) => string.Empty,
                _ when min == max => $"{min:yyyyMMdd}",
                (false, _) => $"{QueryKey}:<={max:yyyyMMdd}",
                (_, false) => $"{QueryKey}:>={min:yyyyMMdd}",
                _ => $"{QueryKey}:{min:yyyyMMdd}..{max:yyyyMMdd}"
            };
        }
    }
    public class CreatedFilter : DateRangeFilter
    {
        protected override string QueryKey => "System.ItemDate";

        public CreatedFilter() : base() {}
        public CreatedFilter(DateRange range) : base(range) {}
    }
    public class ModifiedFilter : DateRangeFilter
    {
        protected override string QueryKey => "System.DateModified";

        public ModifiedFilter() : base() {}
        public ModifiedFilter(DateRange range) : base(range) {}
    }
    public class AccessedFilter : DateRangeFilter
    {
        protected override string QueryKey => "System.DateAccessed";

        public AccessedFilter() : base() {}
        public AccessedFilter(DateRange range) : base(range) {}
    }

    public class SizeRangeFilter : IFilter
    {
        public SizeRange Range { get; }

        public SizeRangeFilter(SizeRange range) => Range = range;

        public string ToAdvancedQuerySyntax()
        {
            var (min, max) = Range;
            bool hasMin = min > Size.MinValue;
            bool hasMax = max < Size.MaxValue;

            return (hasMin, hasMax) switch
            {
                (false, false) => string.Empty,
                _ when min == max => $"{min:yyyyMMdd}",
                (false, _) => $"System.Size:<={max:yyyyMMdd}",
                (_, false) => $"System.Size:>={min:yyyyMMdd}",
                _ => $"System.Size:{min:yyyyMMdd}..{max:yyyyMMdd}"
            };
        }
    }
}
