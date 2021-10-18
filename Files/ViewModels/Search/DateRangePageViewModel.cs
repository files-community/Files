using Files.Extensions;
using Files.Filesystem.Search;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface IDateRangePageViewModel : IFilterPageViewModel, INotifyPropertyChanged
    {
        string RangeLabel { get; }
        DateRange Range { get; set; }

        DateTimeOffset? MinOffset { get; set; }
        DateTimeOffset? MaxOffset { get; set; }

        IReadOnlyList<IDateRangeLink> Links { get; }
    }

    public interface IDateRangeLink : INotifyPropertyChanged
    {
        bool IsSelected { get; }
        string Label { get; }
        ICommand ToggleCommand { get; }
    }

    public class CreatedSource : IFilterSource
    {
        public string Key => "created";
        public string Glyph => "\uE163";
        public string Title => "Created";
        public string Description => "Date of creation";
    }
    public class ModifiedSource : IFilterSource
    {
        public string Key => "modified";
        public string Glyph => "\uE163";
        public string Title => "Modified";
        public string Description => "Date of last modification";
    }
    public class AccessedSource : IFilterSource
    {
        public string Key => "accessed";
        public string Glyph => "\uE163";
        public string Title => "Accessed";
        public string Description => "Date of last access";
    }

    public class DateRangePageViewModel : FilterPageViewModel, IDateRangePageViewModel
    {
        public override IEnumerable<IFilterSource> Sources { get; } = new List<IFilterSource>
        {
            new CreatedSource(),
            new ModifiedSource(),
            new AccessedSource(),
        };

        public string RangeLabel => range.ToString("N");

        public override bool IsEmpty => range == DateRange.Always;

        private DateRange range = DateRange.Always;
        public DateRange Range
        {
            get => range;
            set
            {
                if (SetProperty(ref range, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(MinOffset));
                    OnPropertyChanged(nameof(MaxOffset));
                    OnPropertyChanged(nameof(RangeLabel));

                    links.ForEach(link => link.UpdateProperties());
                }
            }
        }

        public DateTimeOffset? MinOffset
        {
            get => range.MinDate > Date.MinValue ? range.MinDate.Offset : null;
            set
            {
                var minDate = value.HasValue ? new Date(value.Value.DateTime) : Date.MinValue;
                Range = new DateRange(minDate, range.MaxDate);
            }
        }
        public DateTimeOffset? MaxOffset
        {
            get => range.MaxDate < Date.MaxValue ? range.MaxDate.Offset : null;
            set
            {
                var maxDate = value.HasValue ? new Date(value.Value.DateTime) : Date.MaxValue;
                Range = new DateRange(range.MinDate, maxDate);
            }
        }

        private readonly IReadOnlyList<DateRangeLink> links;
        public IReadOnlyList<IDateRangeLink> Links => links;

        public DateRangePageViewModel(FilterCollection parent, DateRangeFilter filter) : base(parent, filter)
        {
            SelectedSource = filter switch
            {
                CreatedFilter => Sources.First(source => source.Key == "created"),
                ModifiedFilter => Sources.First(source => source.Key == "modified"),
                AccessedFilter => Sources.First(source => source.Key == "accessed"),
                _ => SelectedSource,
            };
            Range = filter.Range;

            links = new List<DateRange>
            {
                DateRange.Today,
                DateRange.Yesterday,
                DateRange.ThisWeek,
                DateRange.LastWeek,
                DateRange.ThisMonth,
                DateRange.LastMonth,
                DateRange.ThisYear,
                DateRange.Older,
            }.Select(range => new DateRangeLink(this, range)).ToList().AsReadOnly();
        }

        public override void Clear() => Range = DateRange.Always;

        protected override IFilter CreateFilter() => SelectedSource.Key switch
        {
            "created" => new CreatedFilter(Range),
            "modified" => new ModifiedFilter(Range),
            "accessed" => new AccessedFilter(Range),
            _ => throw new ArgumentException(),
        };

        private class DateRangeLink : ObservableObject, IDateRangeLink
        {
            private readonly IDateRangePageViewModel viewModel;
            private readonly DateRange range;

            public bool IsSelected
            {
                get => !viewModel.IsEmpty && viewModel.Range.IsNamed && viewModel.Range.Contains(range);
                set
                {
                    if (IsSelected != value)
                    {
                        Toggle();
                    }
                }
            }

            public string Label => range.ToString("n");

            public ICommand ToggleCommand { get; }

            public DateRangeLink(IDateRangePageViewModel viewModel, DateRange range)
            {
                this.viewModel = viewModel;
                this.range = range;
                ToggleCommand = new RelayCommand(Toggle);
            }

            public void UpdateProperties() => OnPropertyChanged(nameof(IsSelected));

            private void Toggle()
            {
                if (viewModel.IsEmpty)
                {
                    viewModel.Range = range;
                }
                else if (IsSelected)
                {
                    viewModel.Range -= range;
                }
                else if (viewModel.Range.IsNamed)
                {
                    viewModel.Range += range;
                }
                else
                {
                    viewModel.Range = range;
                }
            }
        }
    }
}
