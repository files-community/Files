using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Enums;
using Files.Extensions;
using Microsoft.Toolkit.Uwp;
using System.Collections.Generic;
using System.Linq;

namespace Files.Filesystem.Search
{
    public interface ISizeFilter : ISearchFilter
    {
        SizeRange Range { get; set; }
    }

    [SearchHeader]
    internal class SizeHeader : ISearchHeader
    {
        public SearchKeys Key => SearchKeys.Size;

        public string Glyph => "\uE2B2";
        public string Label => "Size".GetLocalized();
        public string Description => string.Empty;

        public ISearchFilter CreateFilter() => new SizeFilter();
    }

    internal class SizeFilter : ObservableObject, ISizeFilter
    {
        public SearchKeys Key => SearchKeys.Size;

        public ISearchHeader Header { get; } = Ioc.Default.GetService<ISearchHeaderProvider>().GetHeader(SearchKeys.Size);

        public bool IsEmpty => range == SizeRange.None || range == SizeRange.Limit || range == SizeRange.All;

        private SizeRange range = SizeRange.All;
        public SizeRange Range
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

        public IEnumerable<ISearchTag> Tags => Range.Direction switch
        {
            RangeDirections.EqualTo => new EqualTag(this).CreateEnumerable(),
            RangeDirections.GreaterThan => new FromTag(this).CreateEnumerable(),
            RangeDirections.LessThan => new ToTag(this).CreateEnumerable(),
            RangeDirections.Between => new List<ISearchTag> { new FromTag(this), new ToTag(this) },
            _ => Enumerable.Empty<ISearchTag>(),
        };

        public SizeFilter() {}
        public SizeFilter(SizeRange range) => Range = range;

        public void Clear() => Range = SizeRange.All;

        public string ToAdvancedQuerySyntax()
        {
            var (direction, minValue, maxValue) = Range;

            return direction switch
            {
                RangeDirections.EqualTo => $"System.Size:={minValue.Bytes}",
                RangeDirections.LessThan => $"System.Size:<={maxValue.Bytes}",
                RangeDirections.GreaterThan => $"System.Size:>={minValue.Bytes}",
                RangeDirections.Between => $"System.Size:{minValue.Bytes}..{maxValue.Bytes}",
                _ => string.Empty,
            };
        }

        private class EqualTag : ISearchTag
        {
            ISearchFilter ISearchTag.Filter => Filter;
            public ISizeFilter Filter { get; }

            public string Title => string.Empty;
            public string Parameter => Filter.Range.Label.MinValue;

            public EqualTag(ISizeFilter filter) => Filter = filter;

            public void Delete() => Filter.Range = SizeRange.All;
        }
        private class FromTag : ISearchTag
        {
            ISearchFilter ISearchTag.Filter => Filter;
            public ISizeFilter Filter { get; }

            public string Title => "Range_From".GetLocalized();
            public string Parameter => Filter.Range.Label.MinValue;

            public FromTag(ISizeFilter filter) => Filter = filter;

            public void Delete() => Filter.Range = new SizeRange(Size.MinValue, Filter.Range.MaxValue);
        }
        private class ToTag : ISearchTag
        {
            ISearchFilter ISearchTag.Filter => Filter;
            public ISizeFilter Filter { get; }

            public string Title => "Range_To".GetLocalized();
            public string Parameter => Filter.Range.Label.MaxValue;

            public ToTag(ISizeFilter filter) => Filter = filter;

            public void Delete() => Filter.Range = new SizeRange(Filter.Range.MinValue, Size.MaxValue);
        }
    }
}
