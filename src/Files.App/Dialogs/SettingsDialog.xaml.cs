using Files.Backend.ViewModels.Dialogs;
using Files.App.SettingsPages;
using Files.Shared.Enums;
using Files.App.ViewModels;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
    public sealed partial class SettingsDialog : ContentDialog, IDialog<SettingsDialogViewModel>
    {
        public SettingsDialogViewModel ViewModel
        {
            get => (SettingsDialogViewModel)DataContext;
            set => DataContext = value;
        }

        // for some reason the requested theme wasn't being set on the content dialog, so this is used to manually bind to the requested app theme
        private FrameworkElement RootAppElement => App.Window.Content as FrameworkElement;

        public SettingsDialog()
        {
            this.InitializeComponent();
            SettingsPane.SelectedItem = SettingsPane.MenuItems[0];
            App.Window.SizeChanged += Current_SizeChanged;
            UpdateDialogLayout();
        }


        public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            UpdateDialogLayout();
        }

        private void UpdateDialogLayout()
        {
            if (App.Window.Bounds.Height <= 710)
            {
                ContainerGrid.Height = App.Window.Bounds.Height - 70;
            }
            else
            {
                ContainerGrid.Height = 640;
            }

            if (App.Window.Bounds.Width <= 800)
            {
                ContainerGrid.Width = App.Window.Bounds.Width;
            }
            else
            {
                ContainerGrid.Width = 800;
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
                2 => SettingsContentFrame.Navigate(typeof(Folders)),
                3 => SettingsContentFrame.Navigate(typeof(Multitasking)),
                4 => SettingsContentFrame.Navigate(typeof(Experimental)),
                5 => SettingsContentFrame.Navigate(typeof(About)),
                _ => SettingsContentFrame.Navigate(typeof(Appearance))
            };
        }

        private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            App.Window.SizeChanged -= Current_SizeChanged;
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}