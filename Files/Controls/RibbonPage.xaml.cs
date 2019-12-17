using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Files.Controls
{
    public sealed partial class RibbonPage : UserControl
    {
        public ObservableCollection<UIElement> PageContent { get; set; } = new ObservableCollection<UIElement>();
        public RibbonPage()
        {
            this.InitializeComponent();
            Loaded += RibbonPage_Loaded;
        }

        private void RibbonPage_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (UIElement element in PageContent)
            {
                PageContentPanel.Children.Add(element);
            }
            Loaded -= RibbonPage_Loaded;
        }
    }
}
