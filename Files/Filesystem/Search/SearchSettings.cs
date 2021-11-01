using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Files.Filesystem.Search
{
    public interface ISearchSettings
    {
        ISearchLocation Location { get; }
        ISearchFilter Filter { get; }
    }

    public interface ISearchLocation : INotifyPropertyChanged
    {
        bool SearchInSubFolders { get; set; }
    }

    public interface ISearchFilter
    {
        string ToAdvancedQuerySyntax();
    }

    public interface ISearchFilterCollection : IList<ISearchFilter>, ISearchFilter, IHeader, INotifyPropertyChanged, INotifyCollectionChanged
    {
    }

    public interface IDateRangeFilter : ISearchFilter, IHeader
    {
        DateRange Range { get; }
    }
    public interface ISizeRangeFilter : ISearchFilter, IHeader
    {
        SizeRange Range { get; }
    }

    public interface IHeader
    {
        string Glyph { get; }
        string Title { get; }
        string Description { get; }
    }

    public class SearchSettings : ObservableObject, ISearchSettings
    {
        public static SearchSettings Instance { get; } = new();

        public ISearchLocation Location { get; } = new SearchLocation();
        public ISearchFilter Filter { get; } = new AndFilterCollection();

        private SearchSettings() {}
    }

    public class SearchLocation : ObservableObject, ISearchLocation
    {
        public bool searchInSubFolders = true;
        public bool SearchInSubFolders
        {
            get => searchInSubFolders;
            set => SetProperty(ref searchInSubFolders, value);
        }
    }

    public class AndFilterCollection : ObservableCollection<ISearchFilter>, ISearchFilterCollection
    {
        public string Glyph => "\uEC26";
        public string Title => "SearchAndFilterCollection_Title".GetLocalized();
        public string Description => "SearchAndFilterCollection_Description".GetLocalized();

        public AndFilterCollection() : base() {}
        public AndFilterCollection(IEnumerable<ISearchFilter> filters) : base(filters) {}
        public AndFilterCollection(IList<ISearchFilter> filters) : base(filters) {}

        public string ToAdvancedQuerySyntax()
        {
            var queries = this
                .Where(filter => filter is not null)
                .Select(filter => (filter.ToAdvancedQuerySyntax() ?? string.Empty).Trim())
                .Where(query => !string.IsNullOrEmpty(query))
                .Select(query => query.Contains(' ') ? $"({query})" : query);
            return string.Join(' ', queries);
        }
    }
    public class OrFilterCollection : ObservableCollection<ISearchFilter>, ISearchFilterCollection
    {
        public string Glyph => "\uEC26";
        public string Title => "SearchOrFilterCollection_Title".GetLocalized();
        public string Description => "SearchOrFilterCollection_Description".GetLocalized();

        public OrFilterCollection() : base() {}
        public OrFilterCollection(IEnumerable<ISearchFilter> filters) : base(filters) {}
        public OrFilterCollection(IList<ISearchFilter> filters) : base(filters) {}

        public string ToAdvancedQuerySyntax()
        {
            var queries = this
                .Where(filter => filter is not null)
                .Select(filter => (filter.ToAdvancedQuerySyntax() ?? string.Empty).Trim())
                .Where(query => !string.IsNullOrEmpty(query))
                .Select(query => query.Contains(' ') ? $"({query})" : query);
            return string.Join(" OR ", queries);
        }
    }
    public class NotFilterCollection : ObservableCollection<ISearchFilter>, ISearchFilterCollection
    {
        public string Glyph => "\uEC26";
        public string Title => "SearchNotFilterCollection_Title".GetLocalized();
        public string Description => "SearchNotFilterCollection_Description".GetLocalized();

        public NotFilterCollection() : base() {}
        public NotFilterCollection(IEnumerable<ISearchFilter> filters) : base(filters) {}
        public NotFilterCollection(IList<ISearchFilter> filters) : base(filters) {}

        public string ToAdvancedQuerySyntax()
        {
            var queries = this
                .Where(filter => filter is not null)
                .Select(filter => (filter.ToAdvancedQuerySyntax() ?? string.Empty).Trim())
                .Where(query => !string.IsNullOrEmpty(query))
                .Select(query => query.Contains(' ') ? $"NOT({query})" : query);
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
                _ when min == max => $"{QueryKey}:={min:yyyyMMdd}",
                (false, _) => $"{QueryKey}:<={max:yyyyMMdd}",
                (_, false) => $"{QueryKey}:>={min:yyyyMMdd}",
                _ => $"{QueryKey}:{min:yyyyMMdd}..{max:yyyyMMdd}"
            };
        }
    }
    public class CreatedFilter : DateRangeFilter
    {
        public override string Title => "SearchCreatedFilter_Title".GetLocalized();
        public override string Description => "SearchCreatedFilter_Description".GetLocalized();
        protected override string QueryKey => "System.ItemDate";

        public CreatedFilter() : base() {}
        public CreatedFilter(DateRange range) : base(range) {}
    }
    public class ModifiedFilter : DateRangeFilter
    {
        public override string Title => "SearchModifiedFilter_Title".GetLocalized();
        public override string Description => "SearchModifiedFilter_Description".GetLocalized();
        protected override string QueryKey => "System.DateModified";

        public ModifiedFilter() : base() {}
        public ModifiedFilter(DateRange range) : base(range) {}
    }
    public class AccessedFilter : DateRangeFilter
    {
        public override string Title => "SearchAccessedFilter_Title".GetLocalized();
        public override string Description => "SearchAccessedFilter_Description".GetLocalized();
        protected override string QueryKey => "System.DateAccessed";

        public AccessedFilter() : base() {}
        public AccessedFilter(DateRange range) : base(range) {}
    }

    public class SizeRangeFilter : ISizeRangeFilter
    {
        public string Glyph => "\uE130";
        public string Title => "SearchSizeFilter_Title".GetLocalized();
        public string Description => "SearchSizeFilter_Description".GetLocalized();

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
                _ when min == max => $"System.Size:={min.Bytes}",
                (false, _) => $"System.Size:<={max.Bytes}",
                (_, false) => $"System.Size:>={min.Bytes}",
                _ => $"System.Size:{min.Bytes}..{max.Bytes}"
            };
        }
    }
}
