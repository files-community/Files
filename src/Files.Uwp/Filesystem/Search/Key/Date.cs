using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Enums;
using Files.Extensions;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Files.Filesystem.Search
{
    public interface IDateFilter : IMultiSearchFilter
    {
        DateRange Range { get; set; }
    }

    [SearchHeader]
    internal class DateCreatedHeader : ISearchHeader
    {
        public SearchKeys Key => SearchKeys.DateCreated;

        public string Glyph => "\uEC92";
        public string Label => "DateCreated".GetLocalized();
        public string Description => string.Empty;

        public ISearchFilter CreateFilter() => new DateFilter(Key);
    }

    [SearchHeader]
    internal class DateModifiedHeader : ISearchHeader
    {
        public SearchKeys Key => SearchKeys.DateModified;

        public string Glyph => "\uEC92";
        public string Label => "DateModified".GetLocalized();
        public string Description => string.Empty;

        public ISearchFilter CreateFilter() => new DateFilter(Key);
    }

    [SearchHeader]
    internal class DateAccessedHeader : ISearchHeader
    {
        public SearchKeys Key => SearchKeys.DateAccessed;

        public string Glyph => "\uEC92";
        public string Label => "DateAccessed".GetLocalized();
        public string Description => string.Empty;

        public ISearchFilter CreateFilter() => new DateFilter(Key);
    }

    internal class DateFilter : ObservableObject, IDateFilter
    {
        public SearchKeys Key
        {
            get => header.Key;
            set
            {
                if (value is not SearchKeys.DateCreated and not SearchKeys.DateModified and not SearchKeys.DateAccessed)
                {
                    throw new ArgumentException();
                }
                if (header.Key != value)
                {
                    header = GetHeader(value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Header));
                }
            }
        }

        private ISearchHeader header;
        public ISearchHeader Header => header;

        public bool IsEmpty => range == DateRange.None || range == DateRange.Always;

        private DateRange range = DateRange.Always;
        public DateRange Range
        {
            get => range;
            set
            {
                if (SetProperty(ref range, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(Tags));
                }
            }
        }

        public IEnumerable<ISearchTag> Tags
        {
            get
            {
                var label = Range.Label;
                return label.Direction switch
                {
                    RangeDirections.EqualTo => new EqualTag(this, label).CreateEnumerable(),
                    RangeDirections.GreaterThan => new FromTag(this, label).CreateEnumerable(),
                    RangeDirections.LessThan => new ToTag(this, label).CreateEnumerable(),
                    RangeDirections.Between => new List<ISearchTag> { new FromTag(this, label), new ToTag(this, label) },
                    _ => Enumerable.Empty<ISearchTag>(),
                };
            }
        }

        public DateFilter(SearchKeys key) => header = GetHeader(key);

        public void Clear() => Range = DateRange.Always;

        public string ToAdvancedQuerySyntax()
        {
            string queryKey = Key switch
            {
                SearchKeys.DateCreated => "System.ItemDate",
                SearchKeys.DateModified => "System.DateModified",
                SearchKeys.DateAccessed => "System.DateAccessed",
                _ => throw new InvalidOperationException(),
            };
            var (direction, minValue, maxValue) = Range;

            return direction switch
            {
                RangeDirections.EqualTo => $"{queryKey}:={minValue:yyyy-MM-dd}",
                RangeDirections.LessThan => $"{queryKey}:<={maxValue:yyyy-MM-dd}",
                RangeDirections.GreaterThan => $"{queryKey}:>={minValue:yyyy-MM-dd}",
                RangeDirections.Between => $"{queryKey}:{minValue:yyyy-MM-dd}..{maxValue:yyyy-MM-dd}",
                _ => string.Empty,
            };
        }

        private static ISearchHeader GetHeader(SearchKeys key)
        {
            var provider = Ioc.Default.GetService<ISearchHeaderProvider>();
            return provider.GetHeader(key);
        }

        private class EqualTag : ISearchTag
        {
            ISearchFilter ISearchTag.Filter => Filter;
            public IDateFilter Filter { get; }

            public string Title => string.Empty;
            public string Parameter { get; }

            public EqualTag(IDateFilter filter, IRange<string> label)
                => (Filter, Parameter) = (filter, label.MinValue);

            public void Delete() => Filter.Range = DateRange.Always;
        }
        private class FromTag : ISearchTag
        {
            ISearchFilter ISearchTag.Filter => Filter;
            public IDateFilter Filter { get; }

            public string Title => "Range_From".GetLocalized();
            public string Parameter { get; }

            public FromTag(IDateFilter filter, IRange<string> label)
                => (Filter, Parameter) = (filter, label.MinValue);

            public void Delete() => Filter.Range = new DateRange(Date.MinValue, Filter.Range.MaxValue);
        }
        private class ToTag : ISearchTag
        {
            ISearchFilter ISearchTag.Filter => Filter;
            public IDateFilter Filter { get; }

            public string Title => "Range_To".GetLocalized();
            public string Parameter { get; }

            public ToTag(IDateFilter filter, IRange<string> label)
                => (Filter, Parameter) = (filter, label.MaxValue);

            public void Delete() => Filter.Range = new DateRange(Filter.Range.MinValue, Date.MaxValue);
        }
    }
}
