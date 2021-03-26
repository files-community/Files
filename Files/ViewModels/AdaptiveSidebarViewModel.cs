﻿using Files.Filesystem;
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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels
{
    public class AdaptiveSidebarViewModel : ObservableObject, IDisposable
    {
        public ICommand EmptyRecycleBinCommand { get; private set; }
        public IShellPage ActiveHolderPane { get; set; }
        public IFilesystemHelpers FilesystemHelpers => ActiveHolderPane?.FilesystemHelpers;

        public static readonly GridLength CompactSidebarWidth = SidebarControl.GetSidebarCompactSize();
        public event EventHandler<WindowCompactStateChangedEventArgs> WindowCompactStateChanged;
        private bool isWindowCompactSize;

        public bool IsWindowCompactSize
        {
            get => isWindowCompactSize;
            set
            {
                if (isWindowCompactSize != value)
                {
                    isWindowCompactSize = value;
                    WindowCompactStateChanged?.Invoke(this, new WindowCompactStateChangedEventArgs(isWindowCompactSize));
                    
                    OnPropertyChanged(nameof(IsWindowCompactSize));
                    OnPropertyChanged(nameof(SidebarWidth));
                    OnPropertyChanged(nameof(IsSidebarOpen));
                }
            }
        }

        public void NotifyInstanceRelatedPropertiesChanged()
        {
            OnPropertyChanged(nameof(ActiveHolderPane));
            OnPropertyChanged(nameof(SidebarSelectedItem));

            if (ActiveHolderPane.PaneHolder != null)
            {
                ActiveHolderPane.PaneHolder.UpdateSidebarSelectedItem();
            }
        }

        public bool IsMultiPaneEnabled
        {
            get => App.AppSettings.IsDualPaneEnabled && !IsWindowCompactSize;
        }

        public GridLength SidebarWidth
        {
            get => IsWindowCompactSize || !IsSidebarOpen ? CompactSidebarWidth : App.AppSettings.SidebarWidth;
            set
            {
                if (IsWindowCompactSize || !IsSidebarOpen)
                {
                    return;
                }
                if (App.AppSettings.SidebarWidth != value)
                {
                    App.AppSettings.SidebarWidth = value;
                    OnPropertyChanged(nameof(SidebarWidth));
                }
            }
        }

        public bool IsSidebarOpen
        {
            get => !IsWindowCompactSize && App.AppSettings.IsSidebarOpen;
            set
            {
                if (IsWindowCompactSize)
                {
                    return;
                }
                if (App.AppSettings.IsSidebarOpen != value)
                {
                    App.AppSettings.IsSidebarOpen = value;
                    OnPropertyChanged(nameof(SidebarWidth));
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
            Window.Current.SizeChanged += Current_SizeChanged;
            App.AppSettings.PropertyChanged += AppSettings_PropertyChanged;
            Current_SizeChanged(null, null);
        }

        public void EmptyRecycleBin(RoutedEventArgs e)
        {
            RecycleBinHelpers.EmptyRecycleBin(ActiveHolderPane);
        }

        private void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(App.AppSettings.SidebarWidth):
                    OnPropertyChanged(nameof(SidebarWidth));
                    break;

                case nameof(App.AppSettings.IsSidebarOpen):
                    if (App.AppSettings.IsSidebarOpen != IsSidebarOpen)
                    {
                        OnPropertyChanged(nameof(IsSidebarOpen));
                    }
                    break;
            }
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            if ((Window.Current.Content as Frame).CurrentSourcePageType != typeof(Settings))
            {
                if (IsWindowCompactSize != Window.Current.Bounds.Width <= 750)
                {
                    IsWindowCompactSize = Window.Current.Bounds.Width <= 750;
                }
            }
        }

        public void Dispose()
        {
            Window.Current.SizeChanged -= Current_SizeChanged;
            App.AppSettings.PropertyChanged -= AppSettings_PropertyChanged;
        }
    }

    public class WindowCompactStateChangedEventArgs
    {
        public bool IsWindowCompact { get; set; }

        public WindowCompactStateChangedEventArgs(bool isCompact)
        {
            IsWindowCompact = isCompact;
        }
    }
}
