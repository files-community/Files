using Files.Filesystem.Search;
using System;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls.Search
{
    public sealed partial class CalendarRangePicker : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register(nameof(Range), typeof(DateRange), typeof(CalendarRangePicker), new PropertyMetadata(DateRange.Always));

        public DateRange Range
        {
            get => (DateRange)GetValue(RangeProperty);
            set
            {
                if (Range != value)
                {
                    SetValue(RangeProperty, value);
                    OnPropertyChanged(nameof(MinOffset));
                    OnPropertyChanged(nameof(MaxOffset));
                }
            }
        }

        private DateTimeOffset? MinOffset
        {
            get => Range.MinValue > Date.MinValue ? Range.MinValue.Offset : null;
            set
            {
                var minValue = value.HasValue ? new Date(value.Value.DateTime) : Date.MinValue;
                Range = new DateRange(minValue, Range.MaxValue);
            }
        }
        private DateTimeOffset? MaxOffset
        {
            get => Range.MaxValue <= Date.Today ? Range.MaxValue.Offset : null;
            set
            {
                var maxValue = value.HasValue ? new Date(value.Value.DateTime) : Date.MaxValue;
                Range = new DateRange(Range.MinValue, maxValue);
            }
        }

        public CalendarRangePicker() => InitializeComponent();

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
