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
    public interface ISizeRangePageViewModel : ISearchPageViewModel
    {
        new ISizeRangePickerViewModel Picker { get; }
    }

    public interface ISizeRangePickerViewModel : IPickerViewModel
    {
        string Description { get; }
        string Label { get; }
        SizeRange Range { get; set; }

        IReadOnlyList<ISizeRangeLink> Links { get; }
    }

    public interface ISizeRangeContext : ISearchFilterContext
    {
    }

    public interface ISizeRangeLink : INotifyPropertyChanged
    {
        bool IsSelected { get; }
        string NameLabel { get; }
        string ValueLabel { get; }
        ICommand ToggleCommand { get; }
    }

    public class SizeRangeContext : SearchFilterContext<ISizeRangeFilter>, ISizeRangeContext
    {
        public override string Label => GetFilter().Range.ToString("n");

        public SizeRangeContext(ISearchPageContext parentPageContext, ISizeRangeFilter filter) : base(parentPageContext, filter) {}
    }

    public class SizeRangeHeader : SearchFilterHeader<SizeRangeFilter>
    {
        public SizeRangeFilter GetFilter(SizeRange range) => new(range);
    }

    public class SizeRangePageViewModel : ObservableObject, ISizeRangePageViewModel
    {
        private readonly ISearchPageContext context;

        ISearchPageNavigator ISearchPageViewModel.Navigator => context;

        public ISearchFilterHeader Header { get; } = new SizeRangeHeader();

        IPickerViewModel ISearchPageViewModel.Picker => Picker;
        public ISizeRangePickerViewModel Picker { get; }

        public SizeRangePageViewModel(ISearchPageContext context) : this(context, new SizeRangeFilter())
        {
        }
        public SizeRangePageViewModel(ISearchPageContext context, ISizeRangeFilter filter)
        {
            this.context = context;

            Picker = new PickerViewModel(Save);
            if (filter is not null)
            {
                Picker.Range = filter.Range;
            }
        }

        private void Save() => context.Save(!Picker.IsEmpty ? new SizeRangeFilter(Picker.Range) : null);

        private class PickerViewModel : ObservableObject, ISizeRangePickerViewModel
        {
            private readonly Action saveAction;

            public bool IsEmpty => range == SizeRange.All;

            private SizeRange range = SizeRange.All;
            public SizeRange Range
            {
                get => range;
                set
                {
                    if (value.Equals(SizeRange.None))
                    {
                        value = SizeRange.All;
                    }
                    if (SetProperty(ref range, value))
                    {
                        OnPropertyChanged(nameof(IsEmpty));
                        OnPropertyChanged(nameof(Label));

                        links.ForEach(link => link.UpSizeProperties());

                        saveAction();
                    }
                }
            }

            public string Description { get; }
            public string Label => range.ToString("N");

            private readonly IReadOnlyList<SizeRangeLink> links;
            public IReadOnlyList<ISizeRangeLink> Links => links;

            public ICommand ClearCommand { get; }

            public PickerViewModel(Action saveAction)
            {
                this.saveAction = saveAction;

                Description = new SizeRangeHeader().Description;
                Range = range;

                links = new List<SizeRange>
                {
                    SizeRange.Empty,
                    SizeRange.Tiny,
                    SizeRange.Small,
                    SizeRange.Medium,
                    SizeRange.Large,
                    SizeRange.VeryLarge,
                    SizeRange.Huge,
                }.Select(range => new SizeRangeLink(this, range)).ToList().AsReadOnly();

                ClearCommand = new RelayCommand(Clear);
            }

            public void Clear() => Range = SizeRange.All;

            private class SizeRangeLink : ObservableObject, ISizeRangeLink
            {
                private readonly ISizeRangePickerViewModel picker;
                private readonly SizeRange range;

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

                public string NameLabel => range.ToString("n");
                public string ValueLabel => range.ToString("r");

                public ICommand ToggleCommand { get; }

                public SizeRangeLink(ISizeRangePickerViewModel picker, SizeRange range)
                {
                    this.picker = picker;
                    this.range = range;
                    ToggleCommand = new RelayCommand(Toggle);
                }

                public void UpSizeProperties() => OnPropertyChanged(nameof(IsSelected));

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
}
