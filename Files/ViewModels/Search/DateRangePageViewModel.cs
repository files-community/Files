﻿using Files.Extensions;
using Files.Filesystem.Search;
using Files.UserControls.Search;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface IDateRangePageViewModel : IMultiFilterPageViewModel
    {
        new IDateRangePickerViewModel Picker { get; }
    }

    public interface IDateRangePickerViewModel : IPickerViewModel
    {
        string Description { get; set; }
        string Label { get; }
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

    public class CreatedHeader : IFilterHeader
    {
        public string Glyph => "\uE163";
        public string Title => "Created";
        public string Description => "Date of creation";

        IFilter IFilterHeader.GetFilter() => GetFilter();
        public CreatedFilter GetFilter() => new(DateRange.Always);
        public CreatedFilter GetFilter(DateRange range) => new(range);
    }
    public class ModifiedHeader : IFilterHeader
    {
        public string Glyph => "\uE163";
        public string Title => "Modified";
        public string Description => "Date of last modification";

        IFilter IFilterHeader.GetFilter() => GetFilter();
        public ModifiedFilter GetFilter() => new(DateRange.Always);
        public ModifiedFilter GetFilter(DateRange range) => new(range);
    }
    public class AccessedHeader : IFilterHeader
    {
        public string Glyph => "\uE163";
        public string Title => "Accessed";
        public string Description => "Date of last access";

        IFilter IFilterHeader.GetFilter() => GetFilter();
        public AccessedFilter GetFilter() => new(DateRange.Always);
        public AccessedFilter GetFilter(DateRange range) => new(range);
    }

    public class DateRangePageViewModel : ObservableObject, IDateRangePageViewModel
    {
        public IEnumerable<IFilterHeader> Headers { get; } = new List<IFilterHeader>
        {
            new CreatedHeader(),
            new ModifiedHeader(),
            new AccessedHeader(),
        };

        private IFilterHeader header;
        public IFilterHeader Header
        {
            get => header;
            set
            {
                if (SetProperty(ref header, value))
                {
                    Picker.Description = header.Description;
                }
            }
        }

        IPickerViewModel IFilterPageViewModel.Picker => Picker;
        public IDateRangePickerViewModel Picker { get; } = new DateRangePickerViewModel();

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand AcceptCommand { get; }

        public DateRangePageViewModel()
        {
            BackCommand = new RelayCommand(Back);
            SaveCommand = new RelayCommand(Save);
            AcceptCommand = new RelayCommand(Accept);
        }
        public DateRangePageViewModel(DateRangeFilter filter) : this()
        {
            header = filter switch
            {
                CreatedFilter => Headers.First(h => h is CreatedHeader),
                ModifiedFilter => Headers.First(h => h is ModifiedHeader),
                AccessedFilter => Headers.First(h => h is AccessedHeader),
                _ => Headers.First(),
            };
            Picker.Description = header?.Description;
            if (filter is not null)
            {
                Picker.Range = filter.Range;
            }
        }

        public void Back()
        {
            Navigator.Instance.GoBack();
        }
        public void Save()
        {
        }
        public void Accept()
        {
            Save();
            Back();
        }
    }

    public class DateRangePickerViewModel : ObservableObject, IDateRangePickerViewModel
    {
        public bool IsEmpty => range == DateRange.Always;

        private DateRange range = DateRange.Always;
        public DateRange Range
        {
            get => range;
            set
            {
                if (SetProperty(ref range, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(Label));
                    OnPropertyChanged(nameof(MinOffset));
                    OnPropertyChanged(nameof(MaxOffset));

                    links.ForEach(link => link.UpdateProperties());
                }
            }
        }

        private string description;
        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        public string Label => range.ToString("N");

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

        public ICommand ClearCommand { get; }

        public DateRangePickerViewModel() : this(DateRange.Always)
        {
        }
        public DateRangePickerViewModel(DateRange range)
        {
            Range = range;

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

            ClearCommand = new RelayCommand(Clear);
        }

        public void Clear() => Range = DateRange.Always;

        private class DateRangeLink : ObservableObject, IDateRangeLink
        {
            private readonly IDateRangePickerViewModel picker;
            private readonly DateRange range;

            public bool IsSelected
            {
                get => !picker.IsEmpty && picker.Range.IsNamed && picker.Range.Contains(range);
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

            public DateRangeLink(IDateRangePickerViewModel picker, DateRange range)
            {
                this.picker = picker;
                this.range = range;
                ToggleCommand = new RelayCommand(Toggle);
            }

            public void UpdateProperties() => OnPropertyChanged(nameof(IsSelected));

            private void Toggle()
            {
                if (picker.IsEmpty)
                {
                    picker.Range = range;
                }
                else if (IsSelected)
                {
                    picker.Range -= range;
                }
                else if (picker.Range.IsNamed)
                {
                    picker.Range += range;
                }
                else
                {
                    picker.Range = range;
                }
            }
        }
    }
}
