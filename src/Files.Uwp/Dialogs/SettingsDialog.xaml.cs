﻿using Files.Backend.ViewModels.Dialogs;
using Files.Uwp.SettingsPages;
using Files.Shared.Enums;
using Files.Uwp.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Uwp.Dialogs
{
    public sealed partial class SettingsDialog : ContentDialog, IDialog<SettingsDialogViewModel>
    {
        public SettingsDialogViewModel ViewModel
        {
            get => (SettingsDialogViewModel)DataContext;
            set => DataContext = value;
        }

        // for some reason the requested theme wasn't being set on the content dialog, so this is used to manually bind to the requested app theme
        private FrameworkElement RootAppElement => Window.Current.Content as FrameworkElement;

        public SettingsDialog()
        {
            this.InitializeComponent();
            SettingsPane.SelectedItem = SettingsPane.MenuItems[0];
            Window.Current.SizeChanged += Current_SizeChanged;
            UpdateDialogLayout();
        }

        
        public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            UpdateDialogLayout();
        }

        private void UpdateDialogLayout()
        {
            if (Window.Current.Bounds.Height <= 710)
            {
                ContainerGrid.Height = Window.Current.Bounds.Height - 70;
            }
            else
            {
                ContainerGrid.Height = 640;
            }

            if (Window.Current.Bounds.Width <= 800)
            {
                ContainerGrid.Width = Window.Current.Bounds.Width;
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