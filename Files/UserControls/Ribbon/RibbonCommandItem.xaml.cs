using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.Ribbon
{
    public sealed partial class RibbonCommandItem : UserControl, IRibbonItem
    {
        
        private string ItemLabelText { get; set; }
        private RibbonItemDisplayMode ItemDisplayMode { get; set; } = RibbonItemDisplayMode.Wide;

        public RibbonCommandItem()
        {
            this.InitializeComponent();
            this.Loaded += RibbonCommandItem_Loaded;
        }

        private void RibbonCommandItem_Loaded(object sender, RoutedEventArgs e)
        {
            switch (ItemDisplayMode)
            {
                case RibbonItemDisplayMode.Wide:
                    this.rootAppBarButton.Style = this.Resources["WideRibbonCommandItemStyle"] as Style;
                    break;
                case RibbonItemDisplayMode.Tall:
                    this.rootAppBarButton.Style = this.Resources["TallRibbonCommandItemStyle"] as Style;
                    break;
                case RibbonItemDisplayMode.Compact:
                    this.rootAppBarButton.Style = this.Resources["CompactRibbonCommandItemStyle"] as Style;
                    break;
                case RibbonItemDisplayMode.Divider:
                    throw new NotSupportedException();
                    break;
            }
        }

        public IconElement Icon { get; set; }

        public RibbonItemDisplayMode DisplayMode {
            get
            {
                return ItemDisplayMode;
            }
            set
            {
                switch(value)
                {
                    case RibbonItemDisplayMode.Wide:
                        this.rootAppBarButton.Style = this.Resources["WideRibbonCommandItemStyle"] as Style;
                        break;
                    case RibbonItemDisplayMode.Tall:
                        this.rootAppBarButton.Style = this.Resources["TallRibbonCommandItemStyle"] as Style;
                        break;
                    case RibbonItemDisplayMode.Compact:
                        this.rootAppBarButton.Style = this.Resources["CompactRibbonCommandItemStyle"] as Style;
                        break;
                    case RibbonItemDisplayMode.Divider:
                        throw new NotSupportedException();
                        break;
                }
                ItemDisplayMode = value;
            }
        }
        public string TooltipText { get => ToolTipService.GetToolTip(this).ToString(); set => ToolTipService.SetToolTip(this, value); }
        public string LabelText { get => ItemLabelText; set => ItemLabelText = value; }
        RibbonItemDisplayMode IRibbonItem.DisplayMode { get => DisplayMode; set => DisplayMode = value; }
        string IRibbonItem.TooltipText { get => TooltipText; set => TooltipText = value; }
        string IRibbonItem.LabelText { get => LabelText; set => LabelText = value; }

        double IRibbonItem.EstimatedWidth => this.ActualWidth;
    }
   
}
