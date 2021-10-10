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

    public interface IFilter : INotifyPropertyChanged
    {
        string Key { get; }
        string Glyph { get; }
        string Label { get; }

        bool IsEmpty { get; }
        void Clear();

        string ToAdvancedQuerySyntax();
    }

    public interface IFilterCollection : ICollection<IFilter>, IContainerFilter, INotifyCollectionChanged
    {
    }
    public interface IContainerFilter : IFilter
    {
        void Set(IFilter filter);
        void Unset(IFilter filter);
    }

    public interface IDateRangeFilter : IFilter
    {
        DateRange Range { get; set; }
    }
    public interface ISizeRangeFilter : IFilter
    {
        SizeRange Range { get; set; }
    }

    public class Settings : ObservableObject, ISettings
    {
        public static Settings Default { get; } = new();

        public ILocation Location { get; } = new Location();

        IFilter ISettings.Filter => Filter;
        public AndFilter Filter { get; } = new AndFilter();

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

    public abstract class FilterCollection : ObservableCollection<IFilter>, IFilterCollection
    {
        public bool IsEmpty => Count == 0;

        public abstract string Key { get; }
        public abstract string Glyph { get; }
        public abstract string Label { get; }

        public FilterCollection() : base() {}
        public FilterCollection(IEnumerable<IFilter> filters) : base(filters) {}
        public FilterCollection(IList<IFilter> filters) : base(filters) {}

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsEmpty)));
        }

        public abstract string ToAdvancedQuerySyntax();

        public void Set(IFilter filter)
        {
            if (!Contains(filter))
            {
                Add(filter);
            }
        }
        public void Unset(IFilter filter)
        {
            if (Contains(filter))
            {
                Remove(filter);
            }
        }
    }

    public class AndFilter : FilterCollection
    {
        public override string Key => "and";
        public override string Glyph => "\xE168";
        public override string Label => "And";

        public AndFilter() : base() {}
        public AndFilter(IEnumerable<IFilter> filters) : base(filters) {}
        public AndFilter(IList<IFilter> filters) : base(filters) {}

        public override string ToAdvancedQuerySyntax()
        {
            var queries = Items.Where(filter => !filter.IsEmpty).Select(filter => ToQuery(filter));
            return string.Join(' ', queries);

            static string ToQuery(IFilter filter)
            {
                var query = filter.ToAdvancedQuerySyntax().Trim();
                return query.Contains(' ') ? query : $"({query})";
            }
        }
    }
    public class OrFilter : FilterCollection
    {
        public override string Key => "operator.or";
        public override string Glyph => "\xE168";
        public override string Label => "Or";

        public OrFilter() : base() {}
        public OrFilter(IEnumerable<IFilter> filters) : base(filters) {}
        public OrFilter(IList<IFilter> filters) : base(filters) {}

        public override string ToAdvancedQuerySyntax()
        {
            var queries = Items.Where(filter => !filter.IsEmpty).Select(filter => ToQuery(filter));
            return string.Join(' ', queries);

            static string ToQuery(IFilter filter)
            {
                var query = filter.ToAdvancedQuerySyntax().Trim();
                return query.Contains(" or ") ? query : $"({query})";
            }
        }
    }
    public class NotFilter : ObservableObject, IContainerFilter
    {
        public bool IsEmpty => subFilter is null;

        public string Key => "not";
        public string Glyph => "\xE168";
        public string Label => "Not";

        private IFilter subFilter;
        public IFilter SubFilter
        {
            get => subFilter;
            set
            {
                if (SetProperty(ref subFilter, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public void Clear() => SubFilter = null;

        public string ToAdvancedQuerySyntax() => subFilter switch
        {
            null => string.Empty,
            _ => $"not({subFilter.ToAdvancedQuerySyntax()})"
        };

        public void Set(IFilter filter) => SubFilter = filter;
        public void Unset(IFilter filter) => SubFilter = null;
    }

    public abstract class DateRangeFilter : ObservableObject, IDateRangeFilter
    {
        public bool IsEmpty => range.Equals(DateRange.Always);

        public abstract string Key { get; }
        public virtual string Glyph => "\xE163";
        public abstract string Label { get; }
        protected abstract string QueryLabel { get; }

        private DateRange range = DateRange.Always;
        public DateRange Range
        {
            get => range;
            set
            {
                if (SetProperty(ref range, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public void Clear() => Range = DateRange.Always;

        public string ToAdvancedQuerySyntax()
        {
            var (min, max) = range;
            bool hasMin = min > Date.MinValue;
            bool hasMax = max < Date.Today;

            return (hasMin, hasMax) switch
            {
                (false, false) => string.Empty,
                _ when min == max => $"{min:yyyyMMdd}",
                (false, _) => $"{QueryLabel}:<={max:yyyyMMdd}",
                (_, false) => $"{QueryLabel}:>={min:yyyyMMdd}",
                _ => $"{QueryLabel}:{min:yyyyMMdd}..{max:yyyyMMdd}"
            };
        }
    }
    public class CreatedFilter : DateRangeFilter
    {
        public override string Key => "file.created";
        public override string Label => "Created";
        protected override string QueryLabel => "System.ItemDate";
    }
    public class ModifiedFilter : DateRangeFilter
    {
        public override string Key => "file.modified";
        public override string Label => "Modified";
        protected override string QueryLabel => "System.DateModified";
    }

    public class SizeFilter : ObservableObject, ISizeRangeFilter
    {
        public bool IsEmpty => range.Equals(SizeRange.All);

        public string Key => "file.size";
        public string Glyph => "\xE163";
        public string Label => "File Size";

        private SizeRange range = SizeRange.All;
        public SizeRange Range
        {
            get => range;
            set
            {
                if (SetProperty(ref range, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public void Clear() => Range = SizeRange.All;

        public string ToAdvancedQuerySyntax()
        {
            var (min, max) = range;
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
