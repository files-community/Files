using Files.SettingsPages;
using Files.ViewModels;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Dialogs
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        // for some reason the requested theme wasn't being set on the content dialog, so this is used to manually bind to the requested app theme
        private FrameworkElement RootAppElement => Window.Current.Content as FrameworkElement;

        public SettingsDialog()
        {
            this.InitializeComponent();
            SettingsPane.SelectedItem = SettingsPane.MenuItems[0];
            Window.Current.SizeChanged += Current_SizeChanged;
            UpdateDialogLayout();
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            UpdateDialogLayout();
        }

        private void UpdateDialogLayout()
        {
            if (Window.Current.Bounds.Height <= 600)
            {
                SettingsScrollViewer.Height = Window.Current.Bounds.Height - 40;
            }
            else
            {
                SettingsScrollViewer.Height = 560;
            }
        }

        private void SettingsPane_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)args.SelectedItem;
            int selectedItemTag = Convert.ToInt32(selectedItem.Tag);

            _ = selectedItemTag switch
            {
                0 => SettingsContentFrame.Navigate(typeof(Appearance)),
                1 => SettingsContentFrame.Navigate(typeof(Preferences)),
                2 => SettingsContentFrame.Navigate(typeof(Multitasking)),
                3 => SettingsContentFrame.Navigate(typeof(Experimental)),
                4 => SettingsContentFrame.Navigate(typeof(About)),
                _ => SettingsContentFrame.Navigate(typeof(Appearance))
            };
        }

        private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            Window.Current.SizeChanged -= Current_SizeChanged;
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}