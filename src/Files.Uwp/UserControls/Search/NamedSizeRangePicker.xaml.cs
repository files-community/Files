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
    public sealed partial class NamedSizeRangePicker : UserControl
    {
        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register(nameof(Range), typeof(SizeRange), typeof(NamedSizeRangePicker), new PropertyMetadata(SizeRange.All));

        public SizeRange Range
        {
            get => (SizeRange)GetValue(RangeProperty);
            set
            {
                if (value == SizeRange.None)
                {
                    value = SizeRange.All;
                }
                if (Range != value)
                {
                    SetValue(RangeProperty, value);
                    NamedRanges.ForEach(link => link.UpdateProperties());
                }
            }
        }

        private IEnumerable<NamedSizeRange> NamedRanges { get; }

        public NamedSizeRangePicker()
        {
            InitializeComponent();

            NamedRanges = new List<SizeRange>
            {
                SizeRange.Empty,
                SizeRange.Tiny,
                SizeRange.Small,
                SizeRange.Medium,
                SizeRange.Large,
                SizeRange.VeryLarge,
                SizeRange.Huge,
            }.Select(range => new NamedSizeRange(this, range)).ToList();
        }

        private class NamedSizeRange : ObservableObject, INamedSizeRange
        {
            private readonly NamedSizeRangePicker picker;
            private readonly SizeRange range;

            public string Name => range.Label.ToString();
            public string Value => new RangeLabel(range.MinValue.ToString(), range.MaxValue.ToString()).ToString();

            public bool IsSelected
            {
                get => picker.Range != SizeRange.All && picker.Range.IsNamed && picker.Range.Contains(range);
                set
                {
                    if (IsSelected != value)
                    {
                        Toggle();
                    }
                }
            }

            public NamedSizeRange(NamedSizeRangePicker picker, SizeRange range) => (this.picker, this.range) = (picker, range);

            public void UpdateProperties() => OnPropertyChanged(nameof(IsSelected));

            private void Toggle()
            {
                if (picker.Range == SizeRange.All)
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

    public interface INamedSizeRange : INotifyPropertyChanged
    {
        string Name { get; }
        string Value { get; }
        bool IsSelected { get; set; }
    }
}
