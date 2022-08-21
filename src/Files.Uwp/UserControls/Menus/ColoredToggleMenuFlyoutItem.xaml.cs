using Files.Backend.Models.Coloring;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Uwp.UserControls.Menus
{
    public sealed partial class ColoredToggleMenuFlyoutItem : ToggleMenuFlyoutItem
    {
        public ColoredToggleMenuFlyoutItem()
        {
            this.InitializeComponent();
        }

        public ColorModel ColorModel
        {
            get { return (ColorModel)GetValue(ColorModelProperty); }
            set { SetValue(ColorModelProperty, value); }
        }

        public static readonly DependencyProperty ColorModelProperty =
            DependencyProperty.Register("ColorModel", typeof(ColorModel), typeof(ColoredToggleMenuFlyoutItem), new PropertyMetadata(null));
    }
}
