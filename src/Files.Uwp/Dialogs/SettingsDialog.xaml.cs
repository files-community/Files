using Files.Backend.ViewModels.Dialogs;
using Files.SettingsPages;
using Files.Shared.Enums;
using Files.Uwp.Helpers;
using Files.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Files.Dialogs
{
    public sealed partial class SettingsDialog : ContentDialog, IDialog<SettingsDialogViewModel>, IDialogWithUIContext
    {
        public SettingsDialogViewModel ViewModel
        {
            get => (SettingsDialogViewModel)DataContext;
            set => DataContext = value;
        }

        public UIContext Context { get; set; }

        // for some reason the requested theme wasn't being set on the content dialog, so this is used to manually bind to the requested app theme
        private FrameworkElement RootAppElement;

        public SettingsDialog()
        {
            this.InitializeComponent();
            SettingsPane.SelectedItem = SettingsPane.MenuItems[0];
            this.Loaded += SettingsDialog_Loaded;
        }

        private void SettingsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var content = WindowManagementHelpers.GetWindowContentFromUIContext(Context);
            RootAppElement = content as FrameworkElement;
            RootAppElement.XamlRoot.Changed += XamlRoot_Changed;
            UpdateDialogLayout();
        }

        private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
        {
            UpdateDialogLayout();
        }

        public new async Task<DialogResult> ShowAsync()
        {
            this.XamlRoot = (WindowManagementHelpers.GetWindowFromUIContext(Context) is AppWindow aw)? ElementCompositionPreview.GetAppWindowContent(aw).XamlRoot : Window.Current.Content.XamlRoot;
            return (DialogResult)await base.ShowAsync();
        }

        private void UpdateDialogLayout()
        {
            object window = WindowManagementHelpers.GetWindowFromUIContext(Context);
            if (window is AppWindow aw)
            {
                if (ElementCompositionPreview.GetAppWindowContent(aw).XamlRoot.Size.Height <= 710)
                {
                    ContainerGrid.Height = ElementCompositionPreview.GetAppWindowContent(aw).XamlRoot.Size.Height - 70;
                }
                else
                {
                    ContainerGrid.Height = 640;
                }

                if (ElementCompositionPreview.GetAppWindowContent(aw).XamlRoot.Size.Width <= 800)
                {
                    ContainerGrid.Width = ElementCompositionPreview.GetAppWindowContent(aw).XamlRoot.Size.Width;
                }
                else
                {
                    ContainerGrid.Width = 800;
                }
            }
            else if (window != null)
            {
                if (Window.Current.Content.XamlRoot.Size.Height <= 710)
                {
                    ContainerGrid.Height = Window.Current.Content.XamlRoot.Size.Height - 70;
                }
                else
                {
                    ContainerGrid.Height = 640;
                }

                if (Window.Current.Content.XamlRoot.Size.Width <= 800)
                {
                    ContainerGrid.Width = Window.Current.Content.XamlRoot.Size.Width;
                }
                else
                {
                    ContainerGrid.Width = 800;
                }
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
           RootAppElement.XamlRoot.Changed -= XamlRoot_Changed;
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}