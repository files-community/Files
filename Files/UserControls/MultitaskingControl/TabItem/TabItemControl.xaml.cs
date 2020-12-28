using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls.MultitaskingControl
{
    public sealed partial class TabItemControl : UserControl, ITabItemContainer, IDisposable
    {
        public TabItemArguments NavigationArguments
        {
            get { return (TabItemArguments)GetValue(NavigationArgumentsProperty); }
            set { SetValue(NavigationArgumentsProperty, value); }
        }

        public static readonly DependencyProperty NavigationArgumentsProperty =
            DependencyProperty.Register("NavigationArguments", typeof(TabItemArguments), typeof(TabItemControl), new PropertyMetadata(null, new PropertyChangedCallback(OnNavigationArgumentsChanged)));

        public event EventHandler<TabItemArguments> ContentChanged;

        private static void OnNavigationArgumentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var navArguments = e.NewValue as TabItemArguments;
            if (navArguments != null)
            {
                (d as TabItemControl).ContentFrame.Navigate(navArguments.InitialPageType, navArguments.NavigationArg);
            }
            else
            {
                (d as TabItemControl).ContentFrame.Content = null;
            }
        }

        public void Dispose()
        {
            if (TabItemContent is IDisposable disposableContent)
            {
                disposableContent?.Dispose();
            }
            ContentFrame.Content = null;
        }

        public ITabItemContent TabItemContent => ContentFrame?.Content as ITabItemContent;

        public TabItemControl()
        {
            this.InitializeComponent();
        }

        private void ContentFrame_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (TabItemContent != null)
            {
                TabItemContent.ContentChanged += TabItemContent_ContentChanged;
            }
        }

        private void TabItemContent_ContentChanged(object sender, TabItemArguments e)
        {
            ContentChanged?.Invoke(this, e);
        }
    }
}
