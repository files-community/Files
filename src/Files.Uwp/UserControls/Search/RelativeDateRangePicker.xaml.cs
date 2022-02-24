using CommunityToolkit.Mvvm.ComponentModel;
using Files.Extensions;
using Files.Filesystem.Search;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls.Search
{
    public sealed partial class RelativeDateRangePicker : UserControl
    {
        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register(nameof(Range), typeof(DateRange), typeof(RelativeDateRangePicker), new PropertyMetadata(DateRange.Always));

        public DateRange Range
        {
            get => (DateRange)GetValue(RangeProperty);
            set
            {
                if (value.Equals(DateRange.None))
                {
                    value = DateRange.Always;
                }
                if (Range != value)
                {
                    SetValue(RangeProperty, value);
                    RelativeRanges.ForEach(link => link.UpdateProperties());
                }
            }
        }

        private IEnumerable<RelativeDateRange> RelativeRanges { get; }

        public RelativeDateRangePicker()
        {
            InitializeComponent();

            RelativeRanges = new List<DateRange>
            {
                DateRange.Today,
                DateRange.Yesterday,
                DateRange.ThisWeek,
                DateRange.LastWeek,
                DateRange.ThisMonth,
                DateRange.LastMonth,
                DateRange.ThisYear,
                DateRange.Older,
            }.Select(range => new RelativeDateRange(this, range)).ToList();
        }

        private class RelativeDateRange : ObservableObject, IRelativeDateRange
        {
            private readonly RelativeDateRangePicker picker;
            private readonly DateRange range;

            public string Name => range.Label.ToString();

            public bool IsSelected
            {
                get => picker.Range != DateRange.Always && picker.Range.IsRelative && picker.Range.Contains(range);
                set
                {
                    if (IsSelected != value)
                    {
                        Toggle();
                    }
                }
            }

            public RelativeDateRange(RelativeDateRangePicker picker, DateRange range) => (this.picker, this.range) = (picker, range);

            public void UpdateProperties() => OnPropertyChanged(nameof(IsSelected));

            private void Toggle()
            {
                if (picker.Range == DateRange.Always)
                {
                    picker.Range = range;
                }
                else if (IsSelected)
                {
                    picker.Range -= range;
                }
                else if (picker.Range.IsRelative)
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

    public interface IRelativeDateRange : INotifyPropertyChanged
    {
        string Name { get; }
        bool IsSelected { get; set; }
    }
}
