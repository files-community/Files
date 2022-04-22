﻿using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Uwp.Helpers;
using Files.Uwp.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.Uwp.UserControls
{
    public sealed partial class InnerNavigationToolbar : UserControl
    {
        public InnerNavigationToolbar()
        {
            this.InitializeComponent();
        }

        public IUserSettingsService UserSettingsService { get; } =
            Ioc.Default.GetService<IUserSettingsService>();

        public MainViewModel MainViewModel => App.MainViewModel;

        public ToolbarViewModel ViewModel
        {
            get => (ToolbarViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(ToolbarViewModel), typeof(InnerNavigationToolbar), new PropertyMetadata(null));

        private async void NavToolbarEnterCompactOverlay_Click(object sender, RoutedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.ViewMode == ApplicationViewMode.CompactOverlay)
            {
                await view.TryEnterViewModeAsync(ApplicationViewMode.Default);
                NavToolbarExitCompactOverlay.Visibility = Visibility.Collapsed;
                NavToolbarEnterCompactOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                NavToolbarExitCompactOverlay.Visibility = Visibility.Visible;
                NavToolbarEnterCompactOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void SetAppBarButtonProps(ICommandBarElement e)
        {
            if (e is AppBarButton appBarButton)
            {
                if (appBarButton.Icon is FontIcon bFontIcon)
                {
                    bFontIcon.Style = Resources["AccentColorFontIconStyle"] as Style;
                }
                if (appBarButton.LabelPosition == CommandBarLabelPosition.Collapsed)
                {
                    appBarButton.Width = 48;
                }
            }
            else if (e is AppBarToggleButton appBarToggleButton)
            {
                if (appBarToggleButton.Icon is FontIcon tFontIcon)
                {
                    tFontIcon.Style = Resources["AccentColorFontIconStyle"] as Style;
                }

                if (appBarToggleButton.LabelPosition == CommandBarLabelPosition.Collapsed)
                {
                    appBarToggleButton.Width = 48;
                }
            }
        }

        public bool ShowPreviewPaneButton
        {
            get { return (bool)GetValue(ShowPreviewPaneButtonProperty); }
            set { SetValue(ShowPreviewPaneButtonProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowPreviewPaneButton.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowPreviewPaneButtonProperty =
            DependencyProperty.Register("ShowPreviewPaneButton", typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));

        public bool ShowMultiPaneControls
        {
            get => (bool)GetValue(ShowMultiPaneControlsProperty);
            set => SetValue(ShowMultiPaneControlsProperty, value);
        }

        // Using a DependencyProperty as the backing store for ShowMultiPaneControls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowMultiPaneControlsProperty =
            DependencyProperty.Register(nameof(ShowMultiPaneControls), typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));

        public bool IsMultiPaneActive
        {
            get { return (bool)GetValue(IsMultiPaneActiveProperty); }
            set { SetValue(IsMultiPaneActiveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsMultiPaneActive.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMultiPaneActiveProperty =
            DependencyProperty.Register("IsMultiPaneActive", typeof(bool), typeof(AddressToolbar), new PropertyMetadata(false));

        public bool IsCompactOverlay
        {
            get { return (bool)GetValue(IsCompactOverlayProperty); }
            set { SetValue(IsCompactOverlayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsCompactOverlay.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCompactOverlayProperty =
            DependencyProperty.Register("IsCompactOverlay", typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));

        public ICommand SetCompactOverlayCommand
        {
            get { return (ICommand)GetValue(SetCompactOverlayCommandProperty); }
            set { SetValue(SetCompactOverlayCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ToggleCompactOverlayCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SetCompactOverlayCommandProperty =
            DependencyProperty.Register("ToggleCompactOverlayCommand", typeof(ICommand), typeof(AddressToolbar), new PropertyMetadata(null));

        private void NewEmptySpace_Opening(object sender, object e)
        {
            if (!ViewModel.InstanceViewModel.CanCreateFileInPage)
            {
                var shell = NewEmptySpace.Items.Where(x => (x.Tag as string) == "CreateNewFile").Reverse().ToList();
                shell.ForEach(x => NewEmptySpace.Items.Remove(x));
                return;
            }
            var cachedNewContextMenuEntries = ContextFlyoutItemHelper.CachedNewContextMenuEntries.IsCompletedSuccessfully ? ContextFlyoutItemHelper.CachedNewContextMenuEntries.Result : null;
            if (cachedNewContextMenuEntries == null)
            {
                return;
            }
            if (!NewEmptySpace.Items.Any(x => (x.Tag as string) == "CreateNewFile"))
            {
                var separatorIndex = NewEmptySpace.Items.IndexOf(NewEmptySpace.Items.Single(x => x.Name == "NewMenuFileFolderSeparator"));
                foreach (var newEntry in Enumerable.Reverse(cachedNewContextMenuEntries))
                {
                    MenuFlyoutItem menuLayoutItem;
                    if (!string.IsNullOrEmpty(newEntry.IconBase64))
                    {
                        byte[] bitmapData = Convert.FromBase64String(newEntry.IconBase64);
                        using var ms = new MemoryStream(bitmapData);
                        var image = new BitmapImage();
                        _ = image.SetSourceAsync(ms.AsRandomAccessStream());
                        menuLayoutItem = new MenuFlyoutItemWithImage()
                        {
                            Text = newEntry.Name,
                            BitmapIcon = image,
                            Tag = "CreateNewFile"
                        };
                    }
                    else
                    {
                        menuLayoutItem = new MenuFlyoutItem()
                        {
                            Text = newEntry.Name,
                            Icon = new FontIcon()
                            {
                                Glyph = "\xE7C3"
                            },
                            Tag = "CreateNewFile"
                        };
                    }
                    menuLayoutItem.Command = ViewModel.CreateNewFileCommand;
                    menuLayoutItem.CommandParameter = newEntry;
                    NewEmptySpace.Items.Insert(separatorIndex + 1, menuLayoutItem);
                }
            }
        }

        private void NavToolbarDetailsHeader_Tapped(object sender, TappedRoutedEventArgs e)
            => ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView(true);
        private void NavToolbarTilesHeader_Tapped(object sender, TappedRoutedEventArgs e)
            => ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeTiles(true);
        private void NavToolbarSmallIconsHeader_Tapped(object sender, TappedRoutedEventArgs e)
            => ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall(true);
        private void NavToolbarMediumIconsHeader_Tapped(object sender, TappedRoutedEventArgs e)
            => ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewMedium(true);
        private void NavToolbarLargeIconsHeader_Tapped(object sender, TappedRoutedEventArgs e)
            => ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewLarge(true);
        private void NavToolbarColumnsHeader_Tapped(object sender, TappedRoutedEventArgs e)
            => ViewModel.InstanceViewModel.FolderSettings.ToggleLayoutModeColumnView(true);
    }
}