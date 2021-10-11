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
    public interface IDateRangeFilterViewModel : IFilterViewModel
    {
        new IDateRangeFilter Filter { get; }

        string ShortRangeLabel { get; }
        string FullRangeLabel { get; }

        DateTimeOffset? MinDateTime { get; set; }
        DateTimeOffset? MaxDateTime { get; set; }

        IReadOnlyList<IDateRangeLink> Links { get; }
    }

    public interface IDateRangeLink : INotifyPropertyChanged
    {
        bool IsSelected { get; }
        DateRange Range { get; }
        string Label { get; }
        ICommand ToggleCommand { get; }
    }

    public class DateRangeFilterViewModel : FilterViewModel<IDateRangeFilter>, IDateRangeFilterViewModel
    {
        public string ShortRangeLabel => Filter.Range.ToString("n");
        public string FullRangeLabel => Filter.Range.ToString("N");

        public DateTimeOffset? MinDateTime
        {
            get
            {
                var minDate = Filter.Range.MinDate;
                return minDate != Date.MinValue ? minDate.Offset : null;
            }
            set
            {
                var minDate = value.HasValue ? new Date(value.Value.DateTime) : Date.MinValue;
                Filter.Range = new(minDate, Filter.Range.MaxDate);
            }
        }
        public DateTimeOffset? MaxDateTime
        {
            get
            {
                var maxDate = Filter.Range.MaxDate;
                return maxDate != Date.MaxValue ? maxDate.Offset : null;
            }
            set
            {
                var maxDate = value.HasValue ? new Date(value.Value.DateTime) : DateRange.Today.MinDate;
                Filter.Range = new(Filter.Range.MinDate, maxDate);
            }
        }

        private readonly Lazy<IReadOnlyList<IDateRangeLink>> links;
        public IReadOnlyList<IDateRangeLink> Links => links.Value;

        public DateRangeFilterViewModel(IDateRangeFilter filter) : base(filter)
        {
            links = new(GetLinks);
            filter.PropertyChanged += Filter_PropertyChanged;
        }

        private IReadOnlyList<IDateRangeLink> GetLinks() => new List<DateRange>
        {
            DateRange.Today,
            DateRange.Yesterday,
            DateRange.ThisWeek,
            DateRange.LastWeek,
            DateRange.ThisMonth,
            DateRange.LastMonth,
            DateRange.ThisYear,
            DateRange.Older,
        }.Select(range => new DateRangeLink(Filter, range)).Cast<IDateRangeLink>().ToList().AsReadOnly();

        private void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IDateRangeFilter.Range))
            {
                if (Filter.Range.Equals(DateRange.None))
                {
                    Filter.Range = DateRange.Always;
                }
                else
                {
                    OnPropertyChanged(nameof(ShortRangeLabel));
                    OnPropertyChanged(nameof(FullRangeLabel));
                    OnPropertyChanged(nameof(MinDateTime));
                    OnPropertyChanged(nameof(MaxDateTime));
                }
            }
        }

        private class DateRangeLink : ObservableObject, IDateRangeLink
        {
            private readonly IDateRangeFilter filter;

            public DateRange Range { get; set; }

            public string Label => Range.ToString("N");

            private bool isSelected = false;
            public bool IsSelected
            {
                get => isSelected;
                set => SetProperty(ref isSelected, value);
            }

            public ICommand ToggleCommand { get; set; }

            public DateRangeLink(IDateRangeFilter filter, DateRange range)
            {
                this.filter = filter;

                IsSelected = GetIsSelected();
                Range = range;
                ToggleCommand = new RelayCommand(Toggle);

                filter.PropertyChanged += Filter_PropertyChanged;
            }

            private bool GetIsSelected()
                => !filter.IsEmpty && filter.Range.IsNamed && filter.Range.Contains(Range);

            private void Toggle()
            {
                if (filter.IsEmpty)
                {
                    filter.Range = Range;
                }
                else if (IsSelected)
                {
                    filter.Range -= Range;
                }
                else if (filter.Range.IsNamed)
                {
                    filter.Range += Range;
                }
                else
                {
                    filter.Range = Range;
                }
            }

            private void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(IDateRangeFilter.Range))
                {
                    IsSelected = GetIsSelected();
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }
    }
}
