using Files.Filesystem;
using Files.Helpers;
using Files.UserControls;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels
{
    public class AdaptiveSidebarViewModel : ObservableObject, IDisposable
    {
        public ICommand EmptyRecycleBinCommand { get; private set; }
        public IPaneHolder PaneHolder { get; set; }
        public IFilesystemHelpers FilesystemHelpers => PaneHolder?.FilesystemHelpers;

        public static readonly GridLength CompactSidebarWidth = SidebarControl.GetSidebarCompactSize();

        private Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode sidebarDisplayMode;
        
        public Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode SidebarDisplayMode
        {
            get => sidebarDisplayMode;
            set 
            { 
                if(SetProperty(ref sidebarDisplayMode, value))
                {
                    OnPropertyChanged(nameof(IsSidebarCompactSize));
                }
            }
        }

        public bool IsSidebarCompactSize => SidebarDisplayMode != Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Compact && SidebarDisplayMode != Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal;

        public void NotifyInstanceRelatedPropertiesChanged(string arg)
        {
            UpdateSidebarSelectedItemFromArgs(arg);

            OnPropertyChanged(nameof(SidebarSelectedItem));
        }

        public void UpdateSidebarSelectedItemFromArgs(string arg)
        {
            var value = arg;

            INavigationControlItem item = null;
            List<INavigationControlItem> sidebarItems = UserControls.SidebarControl.SideBarItems
                .Where(x => !string.IsNullOrWhiteSpace(x.Path))
                .Concat(UserControls.SidebarControl.SideBarItems.Where(x => (x as LocationItem)?.ChildItems != null).SelectMany(x => (x as LocationItem).ChildItems).Where(x => !string.IsNullOrWhiteSpace(x.Path)))
                .ToList();

            if (string.IsNullOrEmpty(value))
            {
                //SidebarSelectedItem = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home"));
                return;
            }

            item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value + "\\", StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => value.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(Path.GetPathRoot(value), StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                if (value == "NewTab".GetLocalized())
                {
                    item = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home"));
                }
            }

            if (SidebarSelectedItem != item)
            {
                SidebarSelectedItem = item;
            }
        }

        public bool IsMultiPaneEnabled
        {
            get => App.AppSettings.IsDualPaneEnabled && !IsSidebarCompactSize;
        }

        public bool IsSidebarOpen
        {
            get => App.AppSettings.IsSidebarOpen;
            set
            {
                if (App.AppSettings.IsSidebarOpen != value)
                {
                    App.AppSettings.IsSidebarOpen = value;
                    OnPropertyChanged(nameof(IsSidebarOpen));
                }
            }
        }

        private INavigationControlItem selectedSidebarItem;

        public INavigationControlItem SidebarSelectedItem
        {
            get => selectedSidebarItem;
            set => SetProperty(ref selectedSidebarItem, value);
        }

        public AdaptiveSidebarViewModel()
        {
            EmptyRecycleBinCommand = new RelayCommand<RoutedEventArgs>(EmptyRecycleBin);
            App.AppSettings.PropertyChanged += AppSettings_PropertyChanged;
        }

        public void EmptyRecycleBin(RoutedEventArgs e)
        {
            RecycleBinHelpers.EmptyRecycleBin(PaneHolder.ActivePane);
        }

        private void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(App.AppSettings.IsSidebarOpen):
                    if (App.AppSettings.IsSidebarOpen != IsSidebarOpen)
                    {
                        OnPropertyChanged(nameof(IsSidebarOpen));
                    }
                    break;
            }
        }

        public void Dispose()
        {
            App.AppSettings.PropertyChanged -= AppSettings_PropertyChanged;
        }

        public void SidebarControl_DisplayModeChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs args)
        {
            SidebarDisplayMode = args.DisplayMode;
        }
    }
}