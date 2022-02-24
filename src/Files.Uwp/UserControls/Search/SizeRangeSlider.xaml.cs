using Files.Filesystem.Search;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls.Search
{
    public sealed partial class SizeRangeSlider : UserControl
    {
        private Grid ToolTipGrid;
        private TextBlock ToolTipBlock;

        private bool isInUpdateRange = false;

        private readonly IList<Size> Steps = new List<Size>
        {
            new Size(0),
            new Size(1),
            new Size(256),
            new Size(1, Size.Units.Kibi),
            new Size(16, Size.Units.Kibi),
            new Size(256, Size.Units.Kibi),
            new Size(1, Size.Units.Mebi),
            new Size(5, Size.Units.Mebi),
            new Size(16, Size.Units.Mebi),
            new Size(64, Size.Units.Mebi),
            new Size(128, Size.Units.Mebi),
            new Size(256, Size.Units.Mebi),
            new Size(512, Size.Units.Mebi),
            new Size(1, Size.Units.Gibi),
            new Size(5, Size.Units.Gibi),
            new Size(16, Size.Units.Gibi),
            new Size(64, Size.Units.Gibi),
            Size.MaxValue,
        };

        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register(nameof(Range), typeof(SizeRange), typeof(SizeRangeSlider), new PropertyMetadata(null));

        private static readonly DependencyProperty ToolTipProperty =
            DependencyProperty.Register(nameof(ToolTip), typeof(string), typeof(SizeRangeSlider), new PropertyMetadata(null));

        public SizeRange Range
        {
            get => (SizeRange)GetValue(RangeProperty);
            set
            {
                if (Range.Equals(value))
                {
                    return;
                }

                SetValue(RangeProperty, value);

                var (minValue, maxValue) = (value.MinValue, value.MaxValue);

                int minStep = Steps.Contains(minValue) ? 3 * Steps.IndexOf(minValue) : 3 * Steps.IndexOf(Steps.Last(step => step <= minValue)) + 1;
                int maxStep = Steps.Contains(maxValue) ? 3 * Steps.IndexOf(maxValue) : 3 * Steps.IndexOf(Steps.Last(step => step <= maxValue)) + 2;

                isInUpdateRange = true;
                if (Selector.RangeStart > maxStep)
                {
                    Selector.RangeStart = minStep;
                    Selector.RangeEnd = maxStep;
                }
                else
                {
                    Selector.RangeEnd = maxStep;
                    Selector.RangeStart = minStep;
                }
                isInUpdateRange = false;
            }
        }

        private string ToolTip
        {
            get => (string)GetValue(ToolTipProperty);
            set
            {
                SetValue(ToolTipProperty, value);
                if (ToolTipBlock is not null)
                {
                    int index = int.Parse(value);
                    var size = (index % 3) switch
                    {
                        1 => Range.MinValue,
                        2 => Range.MaxValue,
                        _ => Steps[index / 3],
                    };
                    ToolTipBlock.Text = size.ToString("N");

                    // show fake tooltip inside popup.
                    var margin = ToolTipGrid.Margin;
                    margin.Left = 16 * (1 - index / Steps.Count);
                    ToolTipGrid.Margin = margin;
                    ToolTipGrid.UpdateLayout();
                }
            }
        }

        public SizeRangeSlider()
        {
            InitializeComponent();
            SetValue(RangeProperty, SizeRange.All);

            Selector.Minimum = 0;
            Selector.Maximum = 3 * (Steps.Count - 1);
            Selector.RangeStart = Selector.Minimum;
            Selector.RangeEnd = Selector.Maximum;
            Selector.ValueChanged += Selector_ValueChanged;
        }

        private void ToolTip_Loaded(object sender, RoutedEventArgs _) => ToolTipGrid = sender as Grid;
        private void ToolTipBlock_Loaded(object sender, RoutedEventArgs _) => ToolTipBlock = sender as TextBlock;

        private void Selector_ValueChanged(object sender, RangeChangedEventArgs e)
        {
            if (!isInUpdateRange)
            {
                Range = e.ChangedRangeProperty switch
                {
                    RangeSelectorProperty.MinimumValue => new SizeRange(Steps[((int)Selector.RangeStart) / 3], Range.MaxValue),
                    RangeSelectorProperty.MaximumValue => new SizeRange(Range.MinValue, Steps[((int)Selector.RangeEnd) / 3]),
                    _ => throw new ArgumentException(),
                };
            }
        }
    }
}
