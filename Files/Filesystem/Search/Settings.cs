using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

    public interface IFilterCollection : IList<IFilter>, IFilter, INotifyPropertyChanged, INotifyCollectionChanged
    {
    }

    public interface IDateRangeFilter : IFilter, IHeader
    {
        DateRange Range { get; }
    }
    public interface ISizeRangeFilter : IFilter, IHeader
    {
        SizeRange Range { get; }
    }

    public interface IHeader
    {
        string Glyph { get; }
        string Title { get; }
        string Description { get; }
    }

    public class Settings : ObservableObject, ISettings
    {
        public static Settings Instance { get; } = new();

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

    public abstract class FilterCollection : ObservableCollection<IFilter>, IFilterCollection, IHeader
    {
        public virtual string Glyph => "\uEC26";
        public abstract string Title { get; }
        public abstract string Description { get; }

        public FilterCollection() : base() {}
        public FilterCollection(IEnumerable<IFilter> filters) : base(filters) {}
        public FilterCollection(IList<IFilter> filters) : base(filters) {}

        public abstract string ToAdvancedQuerySyntax();
    }
    public class AndFilterCollection : FilterCollection
    {
        public override string Title => "And";
        public override string Description => "Finds items that meet all the conditions in the list.";

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
        public override string Title => "Or";
        public override string Description => "Finds items that meet at least one condition in the list.";

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
        public override string Title => "Not";
        public override string Description => "Finds items that do not meet any condition in the list.";

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

    public abstract class DateRangeFilter : IDateRangeFilter
    {
        public virtual string Glyph => "\uE163";
        public abstract string Title { get; }
        public abstract string Description { get; }

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
        public override string Title => "Created";
        public override string Description => "Date of creation";

        protected override string QueryKey => "System.ItemDate";

        public CreatedFilter() : base() {}
        public CreatedFilter(DateRange range) : base(range) {}
    }
    public class ModifiedFilter : DateRangeFilter
    {
        public override string Title => "Modified";
        public override string Description => "Date of last modification";

        protected override string QueryKey => "System.DateModified";

        public ModifiedFilter() : base() {}
        public ModifiedFilter(DateRange range) : base(range) {}
    }
    public class AccessedFilter : DateRangeFilter
    {
        public override string Title => "Accessed";
        public override string Description => "Date of last access";

        protected override string QueryKey => "System.DateAccessed";

        public AccessedFilter() : base() {}
        public AccessedFilter(DateRange range) : base(range) {}
    }

    public class SizeRangeFilter : ISizeRangeFilter
    {
        public string Glyph => "\uE163";
        public string Title => "Size";
        public string Description => "Size of the item";

        public SizeRange Range { get; }

        public SizeRangeFilter() => Range = SizeRange.All;
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
