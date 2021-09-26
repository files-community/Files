using Files.DataModels.NavigationControlItems;
using Files.DataModels;
using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls;
using Files.UserControls.Widgets;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace Files.Views
{
    /// <summary>
    /// The root page of Files
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public MainViewModel MainViewModel => App.MainViewModel;

        public MainPageViewModel ViewModel
        {
            get => (MainPageViewModel)DataContext;
            set => DataContext = value;
        }

        public SidebarViewModel SidebarAdaptiveViewModel = new SidebarViewModel();

        public OngoingTasksViewModel OngoingTasksViewModel => App.OngoingTasksViewModel;

        public ICommand ToggleFullScreenAcceleratorCommand { get; private set; }

        private ICommand ToggleCompactOverlayCommand => new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(x => ToggleCompactOverlay());
        private ICommand SetCompactOverlayCommand => new RelayCommand<bool>(x => SetCompactOverlay(x));

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            CoreTitleBar.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;
            var flowDirectionSetting = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                FlowDirection = FlowDirection.RightToLeft;
            }
            AllowDrop = true;

            ToggleFullScreenAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(ToggleFullScreenAccelerator);

            App.AppSettings.PropertyChanged += AppSettings_PropertyChanged;
            App.DrivesManager.RefreshCompleted += DrivesManager_RefreshCompleted;
            App.DrivesManager.RemoveDrivesSidebarSection += DrivesManager_RemoveDrivesSidebarSection;
            App.CloudDrivesManager.RefreshCompleted += CloudDrivesManager_RefreshCompleted;
            App.CloudDrivesManager.RemoveCloudDrivesSidebarSection += CloudDrivesManager_RemoveCloudDrivesSidebarSection;
            App.NetworkDrivesManager.RefreshCompleted += NetworkDrivesManager_RefreshCompleted;
            App.NetworkDrivesManager.RemoveNetworkDrivesSidebarSection += NetworkDrivesManager_RemoveNetworkDrivesSidebarSection;
            App.WSLDistroManager.RefreshCompleted += WSLDistroManager_RefreshCompleted;
            App.WSLDistroManager.RemoveWslSidebarSection += WSLDistroManager_RemoveWslSidebarSection;
            App.LibraryManager.Libraries.CollectionChanged += Libraries_CollectionChanged;
            App.LibraryManager.RemoveLibrariesSidebarSection += LibraryManager_RemoveLibrariesSidebarSection;
            App.LibraryManager.RefreshCompleted += LibraryManager_RefreshCompleted;
        }

        private async void LibraryManager_RefreshCompleted(object sender, IReadOnlyList<LibraryLocationItem> libraries)
        {
            await CoreApplication.MainView.CoreWindow.DispatcherQueue.EnqueueAsync(async () =>
            {
                await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    var section = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarLibraries".GetLocalized()) as LocationItem;
                    if (App.AppSettings.ShowLibrarySection && section == null)
                    {
                        section = new LocationItem
                        {
                            Text = "SidebarLibraries".GetLocalized(),
                            Section = SectionType.Library,
                            SelectsOnInvoked = false,
                            IconData = SidebarPinnedModel.IconResources?.FirstOrDefault(x => x.Index == Constants.ImageRes.Libraries)?.IconDataBytes,
                            ChildItems = new ObservableCollection<INavigationControlItem>()
                        };
                        var index = (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Favorites) ? 1 : 0); // After favorites section
                        SidebarControl.SideBarItems.BeginBulkOperation();
                        SidebarControl.SideBarItems.Insert(Math.Min(index, SidebarControl.SideBarItems.Count), section);
                        SidebarControl.SideBarItems.EndBulkOperation();
                    }
                }
                finally
                {
                    SidebarControl.SideBarItemsSemaphore.Release();
                }
            });

            Libraries_CollectionChanged(App.LibraryManager.Libraries, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void LibraryManager_RemoveLibrariesSidebarSection(object sender, EventArgs e)
        {
            try
            {
                var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("SidebarLibraries".GetLocalized()) select n).FirstOrDefault();
                if (!App.AppSettings.ShowLibrarySection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }
        }

        private async void Libraries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var librarySection = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarLibraries".GetLocalized()) as LocationItem;
            if (!App.AppSettings.ShowLibrarySection || librarySection == null)
            {
                return;
            }

            var libraries = (sender as IList<LibraryLocationItem>).ToList();
            await CoreApplication.MainView.CoreWindow.DispatcherQueue.EnqueueAsync(async () =>
            {
                await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Replace:
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var lib in e.OldItems.Cast<LibraryLocationItem>())
                            {
                                librarySection.ChildItems.Remove(lib);
                            }
                            if (e.Action == NotifyCollectionChangedAction.Replace)
                            {
                                goto case NotifyCollectionChangedAction.Add;
                            }
                            break;

                        case NotifyCollectionChangedAction.Reset:
                            foreach (var lib in libraries.Where(LibraryManager.IsLibraryOnSidebar))
                            {
                                if (!librarySection.ChildItems.Any(x => x.Path == lib.Path))
                                {
                                    if (await lib.CheckDefaultSaveFolderAccess())
                                    {
                                        lib.IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(lib.Path, 24u);
                                        librarySection.ChildItems.AddSorted(lib);
                                    }
                                }
                            }
                            foreach (var lib in librarySection.ChildItems.ToList())
                            {
                                if (!libraries.Any(x => x.Path == lib.Path))
                                {
                                    librarySection.ChildItems.Remove(lib);
                                }
                            }
                            break;

                        case NotifyCollectionChangedAction.Add:
                            foreach (var lib in e.NewItems.Cast<LibraryLocationItem>().Where(LibraryManager.IsLibraryOnSidebar))
                            {
                                if (await lib.CheckDefaultSaveFolderAccess())
                                {
                                    lib.IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(lib.Path, 24u);
                                    librarySection.ChildItems.AddSorted(lib);
                                }
                            }
                            break;
                    }
                }
                finally
                {
                    SidebarControl.SideBarItemsSemaphore.Release();
                }
            });
        }

        private void WSLDistroManager_RemoveWslSidebarSection(object sender, EventArgs e)
        {
            try
            {
                var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("WSL".GetLocalized()) select n).FirstOrDefault();
                if (!App.AppSettings.ShowWslSection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }
        }

        private async void WSLDistroManager_RefreshCompleted(object sender, List<INavigationControlItem> distros)
        {
            await CoreApplication.MainView.CoreWindow.DispatcherQueue.EnqueueAsync(async () =>
            {
                await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    var section = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "WSL".GetLocalized()) as LocationItem;
                    if (App.AppSettings.ShowWslSection && section == null)
                    {
                        section = new LocationItem()
                        {
                            Text = "WSL".GetLocalized(),
                            Section = SectionType.WSL,
                            SelectsOnInvoked = false,
                            IconSource = new Uri("ms-appx:///Assets/WSL/genericpng.png"),
                            ChildItems = new ObservableCollection<INavigationControlItem>()
                        };
                        var index = (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Favorites) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Library) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Drives) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.CloudDrives) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Network) ? 1 : 0); // After network section
                        SidebarControl.SideBarItems.BeginBulkOperation();
                        SidebarControl.SideBarItems.Insert(Math.Min(index, SidebarControl.SideBarItems.Count), section);
                        SidebarControl.SideBarItems.EndBulkOperation();
                    }

                    if (section != null)
                    {
                        foreach (var distro in distros.ToList()
                        .OrderBy(o => o.Text))
                        {
                            if (!section.ChildItems.Contains(distro))
                            {
                                section.ChildItems.Add(distro);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // WSL Not Supported/Enabled
                }
                finally
                {
                    SidebarControl.SideBarItemsSemaphore.Release();
                }
            });
        }

        private void NetworkDrivesManager_RemoveNetworkDrivesSidebarSection(object sender, EventArgs e)
        {
            try
            {
                var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("SidebarNetworkDrives".GetLocalized()) select n).FirstOrDefault();
                if (!App.AppSettings.ShowNetworkDrivesSection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }
        }

        private async void NetworkDrivesManager_RefreshCompleted(object sender, IReadOnlyList<DriveItem> drives)
        {
            await CoreApplication.MainView.CoreWindow.DispatcherQueue.EnqueueAsync(async () =>
            {
                await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    var section = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarNetworkDrives".GetLocalized()) as LocationItem;
                    if (App.AppSettings.ShowNetworkDrivesSection && section == null)
                    {
                        section = new LocationItem()
                        {
                            Text = "SidebarNetworkDrives".GetLocalized(),
                            Section = SectionType.Network,
                            SelectsOnInvoked = false,
                            IconData = SidebarPinnedModel.IconResources?.FirstOrDefault(x => x.Index == Constants.ImageRes.NetworkDrives)?.IconDataBytes,
                            ChildItems = new ObservableCollection<INavigationControlItem>()
                        };
                        var index = (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Favorites) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Library) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Drives) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.CloudDrives) ? 1 : 0); // After cloud section
                        SidebarControl.SideBarItems.BeginBulkOperation();
                        SidebarControl.SideBarItems.Insert(Math.Min(index, SidebarControl.SideBarItems.Count), section);
                        SidebarControl.SideBarItems.EndBulkOperation();
                    }

                    if (section != null)
                    {
                        foreach (var drive in drives.ToList()
                        .OrderByDescending(o => string.Equals(o.Text, "Network".GetLocalized(), StringComparison.OrdinalIgnoreCase))
                        .ThenBy(o => o.Text))
                        {
                            var resource = SidebarPinnedModel.IconResources?.FirstOrDefault(x => x.Index == Constants.ImageRes.Folder);

                            drive.ThumbnailData = resource?.IconDataBytes;
                            if (!section.ChildItems.Contains(drive))
                            {
                                section.ChildItems.Add(drive);
                            }
                        }
                    }
                }
                finally
                {
                    SidebarControl.SideBarItemsSemaphore.Release();
                }
            });
        }

        private void CloudDrivesManager_RemoveCloudDrivesSidebarSection(object sender, EventArgs e)
        {
            try
            {
                var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("SidebarCloudDrives".GetLocalized()) select n).FirstOrDefault();
                if (!App.AppSettings.ShowCloudDrivesSection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }
        }

        private void DrivesManager_RemoveDrivesSidebarSection(object sender, EventArgs e)
        {
            try
            {
                var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("SidebarDrives".GetLocalized()) select n).FirstOrDefault();
                if (!App.AppSettings.ShowDrivesSection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }
        }

        private async void DrivesManager_RefreshCompleted(object sender, IReadOnlyList<DriveItem> drives)
        {
            await CoreApplication.MainView.CoreWindow.DispatcherQueue.EnqueueAsync(async () =>
            {
                await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    var section = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarDrives".GetLocalized()) as LocationItem;
                    if (App.AppSettings.ShowDrivesSection && section == null)
                    {
                        section = new LocationItem()
                        {
                            Text = "SidebarDrives".GetLocalized(),
                            Section = SectionType.Drives,
                            SelectsOnInvoked = false,
                            IconData = SidebarPinnedModel.IconResources?.FirstOrDefault(x => x.Index == Constants.ImageRes.ThisPC)?.IconDataBytes,
                            ChildItems = new ObservableCollection<INavigationControlItem>()
                        };
                        var index = (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Favorites) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Library) ? 1 : 0); // After libraries section
                        SidebarControl.SideBarItems.BeginBulkOperation();
                        SidebarControl.SideBarItems.Insert(Math.Min(index, SidebarControl.SideBarItems.Count), section);
                        SidebarControl.SideBarItems.EndBulkOperation();
                    }

                    // Sync drives to sidebar
                    if (section != null)
                    {
                        foreach (DriveItem drive in drives.ToList())
                        {
                            if (!section.ChildItems.Contains(drive))
                            {
                                section.ChildItems.Add(drive);
                            }
                        }

                        foreach (DriveItem drive in section.ChildItems.ToList())
                        {
                            if (!drives.Contains(drive))
                            {
                                section.ChildItems.Remove(drive);
                            }
                        }
                    }

                    // Sync drives to drives widget
                    foreach (DriveItem drive in drives.ToList())
                    {
                        if (!DrivesWidget.ItemsAdded.Contains(drive))
                        {
                            if (drive.Type != DriveType.VirtualDrive)
                            {
                                DrivesWidget.ItemsAdded.Add(drive);
                            }
                        }
                    }

                    foreach (DriveItem drive in DrivesWidget.ItemsAdded.ToList())
                    {
                        if (!drives.Contains(drive))
                        {
                            DrivesWidget.ItemsAdded.Remove(drive);
                        }
                    }
                }
                finally
                {
                    SidebarControl.SideBarItemsSemaphore.Release();
                }
            });
        }

        private async void CloudDrivesManager_RefreshCompleted(object sender, IReadOnlyList<DriveItem> drives)
        {
            await CoreApplication.MainView.CoreWindow.DispatcherQueue.EnqueueAsync(async () =>
            {
                await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    var section = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarCloudDrives".GetLocalized()) as LocationItem;
                    if (App.AppSettings.ShowCloudDrivesSection && section == null && drives.Any())
                    {
                        section = new LocationItem()
                        {
                            Text = "SidebarCloudDrives".GetLocalized(),
                            Section = SectionType.CloudDrives,
                            SelectsOnInvoked = false,
                            IconSource = new Uri("ms-appx:///Assets/FluentIcons/CloudDrive.png"),
                            ChildItems = new ObservableCollection<INavigationControlItem>()
                        };
                        var index = (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Favorites) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Library) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Drives) ? 1 : 0); // After drives section
                        SidebarControl.SideBarItems.BeginBulkOperation();
                        SidebarControl.SideBarItems.Insert(Math.Min(index, SidebarControl.SideBarItems.Count), section);
                        SidebarControl.SideBarItems.EndBulkOperation();
                    }

                    if (section != null)
                    {
                        foreach (DriveItem drive in drives.ToList())
                        {
                            if (!section.ChildItems.Contains(drive))
                            {
                                section.ChildItems.Add(drive);
                            }
                        }
                    }
                }
                finally
                {
                    SidebarControl.SideBarItemsSemaphore.Release();
                }
            });
        }

        private void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(App.AppSettings.PreviewPaneEnabled):
                    LoadPreviewPaneChanged();
                    break;
            }
        }

        public UserControl MultitaskingControl => VerticalTabs;

        private void VerticalTabStrip_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            (sender as Button).Flyout.ShowAt(sender as Button);
        }

        private void VerticalTabStripInvokeButton_DragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            (sender as Button).Flyout.ShowAt(sender as Button);
        }

        private void VerticalTabStripInvokeButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(ViewModel.MultitaskingControl is VerticalTabViewControl))
            {
                ViewModel.MultitaskingControl = VerticalTabs;
                ViewModel.MultitaskingControls.Add(VerticalTabs);
                ViewModel.MultitaskingControl.CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;
            }
        }

        private void DragArea_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(sender as Grid);
        }

        private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            RightPaddingColumn.Width = new GridLength(sender.SystemOverlayRightInset);
        }

        private void HorizontalMultitaskingControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(ViewModel.MultitaskingControl is HorizontalMultitaskingControl))
            {
                ViewModel.MultitaskingControl = horizontalMultitaskingControl;
                ViewModel.MultitaskingControls.Add(horizontalMultitaskingControl);
                ViewModel.MultitaskingControl.CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;
            }
            if (AppSettings.IsVerticalTabFlyoutEnabled)
            {
                FindName(nameof(VerticalTabStripInvokeButton));
            }
        }

        public void TabItemContent_ContentChanged(object sender, TabItemArguments e)
        {
            if (SidebarAdaptiveViewModel.PaneHolder != null)
            {
                var paneArgs = e.NavigationArg as PaneNavigationArguments;
                SidebarAdaptiveViewModel.UpdateSidebarSelectedItemFromArgs(SidebarAdaptiveViewModel.PaneHolder.IsLeftPaneActive ?
                    paneArgs.LeftPaneNavPathParam : paneArgs.RightPaneNavPathParam);
                UpdateStatusBarProperties();
                UpdatePreviewPaneProperties();
                UpdateNavToolbarProperties();
            }
        }

        public void MultitaskingControl_CurrentInstanceChanged(object sender, CurrentInstanceChangedEventArgs e)
        {
            if (SidebarAdaptiveViewModel.PaneHolder != null)
            {
                SidebarAdaptiveViewModel.PaneHolder.PropertyChanged -= PaneHolder_PropertyChanged;
            }
            SidebarAdaptiveViewModel.PaneHolder = e.CurrentInstance as IPaneHolder;
            SidebarAdaptiveViewModel.PaneHolder.PropertyChanged += PaneHolder_PropertyChanged;
            SidebarAdaptiveViewModel.NotifyInstanceRelatedPropertiesChanged((e.CurrentInstance.TabItemArguments?.NavigationArg as PaneNavigationArguments).LeftPaneNavPathParam);
            UpdateStatusBarProperties();
            UpdateNavToolbarProperties();
            UpdatePreviewPaneProperties();
            e.CurrentInstance.ContentChanged -= TabItemContent_ContentChanged;
            e.CurrentInstance.ContentChanged += TabItemContent_ContentChanged;
        }

        private void PaneHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SidebarAdaptiveViewModel.NotifyInstanceRelatedPropertiesChanged(SidebarAdaptiveViewModel.PaneHolder.ActivePane?.TabItemArguments?.NavigationArg?.ToString());
            UpdateStatusBarProperties();
            UpdatePreviewPaneProperties();
            UpdateNavToolbarProperties();
        }

        private void UpdateStatusBarProperties()
        {
            if (StatusBarControl != null)
            {
                StatusBarControl.DirectoryPropertiesViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.DirectoryPropertiesViewModel;
                StatusBarControl.SelectedItemsPropertiesViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.SelectedItemsPropertiesViewModel;
            }
        }

        private void UpdateNavToolbarProperties()
        {
            if (NavToolbar != null)
            {
                NavToolbar.ViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.NavToolbarViewModel;
            }

            if (InnerNavigationToolbar != null)
            {
                InnerNavigationToolbar.ViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.NavToolbarViewModel;
                InnerNavigationToolbar.ShowMultiPaneControls = SidebarAdaptiveViewModel.PaneHolder?.IsMultiPaneEnabled ?? false;
                InnerNavigationToolbar.IsMultiPaneActive = SidebarAdaptiveViewModel.PaneHolder?.IsMultiPaneActive ?? false;
            }
        }

        private void UpdatePreviewPaneProperties()
        {
            LoadPreviewPaneChanged();
            if (PreviewPane != null)
            {
                PreviewPane.Model = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn?.SlimContentPage?.PreviewPaneViewModel;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.OnNavigatedTo(e);
            SidebarControl.SidebarItemInvoked += SidebarControl_SidebarItemInvoked;
            SidebarControl.SidebarItemPropertiesInvoked += SidebarControl_SidebarItemPropertiesInvoked;
            SidebarControl.SidebarItemDropped += SidebarControl_SidebarItemDropped;
            SidebarControl.SidebarItemNewPaneInvoked += SidebarControl_SidebarItemNewPaneInvoked;
        }

        private async void SidebarControl_SidebarItemDropped(object sender, SidebarItemDroppedEventArgs e)
        {
            await SidebarAdaptiveViewModel.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.Package, e.ItemPath, false, true);
        }

        private async void SidebarControl_SidebarItemPropertiesInvoked(object sender, SidebarItemPropertiesInvokedEventArgs e)
        {
            if (e.InvokedItemDataContext is DriveItem)
            {
                await FilePropertiesHelpers.OpenPropertiesWindowAsync(e.InvokedItemDataContext, SidebarAdaptiveViewModel.PaneHolder.ActivePane);
            }
            else if (e.InvokedItemDataContext is LibraryLocationItem library)
            {
                await FilePropertiesHelpers.OpenPropertiesWindowAsync(new LibraryItem(library), SidebarAdaptiveViewModel.PaneHolder.ActivePane);
            }
            else if (e.InvokedItemDataContext is LocationItem locationItem)
            {
                ListedItem listedItem = new ListedItem(null)
                {
                    ItemPath = locationItem.Path,
                    ItemName = locationItem.Text,
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemType = "FileFolderListItem".GetLocalized(),
                    LoadFolderGlyph = true
                };
                await FilePropertiesHelpers.OpenPropertiesWindowAsync(listedItem, SidebarAdaptiveViewModel.PaneHolder.ActivePane);
            }
        }

        private void SidebarControl_SidebarItemNewPaneInvoked(object sender, SidebarItemNewPaneInvokedEventArgs e)
        {
            if (e.InvokedItemDataContext is INavigationControlItem navItem)
            {
                SidebarAdaptiveViewModel.PaneHolder.OpenPathInNewPane(navItem.Path);
            }
        }

        private void SidebarControl_SidebarItemInvoked(object sender, SidebarItemInvokedEventArgs e)
        {
            var invokedItemContainer = e.InvokedItemContainer;

            string navigationPath; // path to navigate
            Type sourcePageType = null; // type of page to navigate

            switch ((invokedItemContainer.DataContext as INavigationControlItem).ItemType)
            {
                case NavigationControlItemType.Location:
                    {
                        var ItemPath = (invokedItemContainer.DataContext as INavigationControlItem).Path; // Get the path of the invoked item

                        if (string.IsNullOrEmpty(ItemPath)) // Section item
                        {
                            navigationPath = invokedItemContainer.Tag?.ToString();
                        }
                        else if (ItemPath.Equals("Home".GetLocalized(), StringComparison.OrdinalIgnoreCase)) // Home item
                        {
                            if (ItemPath.Equals(SidebarAdaptiveViewModel.SidebarSelectedItem?.Path, StringComparison.OrdinalIgnoreCase))
                            {
                                return; // return if already selected
                            }

                            navigationPath = "NewTab".GetLocalized();
                            sourcePageType = typeof(WidgetsPage);
                        }
                        else // Any other item
                        {
                            navigationPath = invokedItemContainer.Tag?.ToString();
                        }

                        break;
                    }
                default:
                    {
                        navigationPath = invokedItemContainer.Tag?.ToString();
                        break;
                    }
            }

            SidebarAdaptiveViewModel.PaneHolder.ActivePane?.NavigateToPath(navigationPath, sourcePageType);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (SidebarControl != null)
            {
                SidebarControl.SidebarItemInvoked -= SidebarControl_SidebarItemInvoked;
                SidebarControl.SidebarItemPropertiesInvoked -= SidebarControl_SidebarItemPropertiesInvoked;
                SidebarControl.SidebarItemDropped -= SidebarControl_SidebarItemDropped;
                SidebarControl.SidebarItemNewPaneInvoked -= SidebarControl_SidebarItemNewPaneInvoked;
            }
            App.DrivesManager.RefreshCompleted -= DrivesManager_RefreshCompleted;
            App.DrivesManager.RemoveDrivesSidebarSection -= DrivesManager_RemoveDrivesSidebarSection;
            App.CloudDrivesManager.RefreshCompleted -= CloudDrivesManager_RefreshCompleted;
            App.CloudDrivesManager.RemoveCloudDrivesSidebarSection -= CloudDrivesManager_RemoveCloudDrivesSidebarSection;
            App.NetworkDrivesManager.RefreshCompleted -= NetworkDrivesManager_RefreshCompleted;
            App.NetworkDrivesManager.RemoveNetworkDrivesSidebarSection -= NetworkDrivesManager_RemoveNetworkDrivesSidebarSection;
            App.WSLDistroManager.RefreshCompleted -= WSLDistroManager_RefreshCompleted;
            App.WSLDistroManager.RemoveWslSidebarSection -= WSLDistroManager_RemoveWslSidebarSection;
            App.LibraryManager.Libraries.CollectionChanged -= Libraries_CollectionChanged;
            App.LibraryManager.RemoveLibrariesSidebarSection -= LibraryManager_RemoveLibrariesSidebarSection;
            App.LibraryManager.RefreshCompleted -= LibraryManager_RefreshCompleted;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.UI.Xaml.Controls.BackdropMaterial.SetApplyToRootOrPageBackground(sender as Control, true);

            // Defers the status bar loading until after the page has loaded to improve startup perf
            FindName(nameof(StatusBarControl));
            FindName(nameof(InnerNavigationToolbar));
            FindName(nameof(horizontalMultitaskingControl));
            FindName(nameof(NavToolbar));

            // the adaptive triggers do not evaluate on app startup, manually checking and calling GoToState here fixes https://github.com/files-community/Files/issues/5801
            if (Window.Current.Bounds.Width < CollapseSearchBoxAdaptiveTrigger.MinWindowWidth)
            {
                _ = VisualStateManager.GoToState(this, nameof(CollapseSearchBoxState), true);
            }

            if (Window.Current.Bounds.Width < MinimalSidebarAdaptiveTrigger.MinWindowWidth)
            {
                _ = VisualStateManager.GoToState(this, nameof(MinimalSidebarState), true);
            }

            if (Window.Current.Bounds.Width < CollapseHorizontalTabViewTrigger.MinWindowWidth)
            {
                _ = VisualStateManager.GoToState(this, nameof(HorizontalTabViewCollapsed), true);
            }

            App.LoadOtherStuffAsync().ContinueWith(t => App.Logger.Warn(t.Exception, "Error during LoadOtherStuffAsync()"), TaskContinuationOptions.OnlyOnFaulted);
        }

        private void ToggleFullScreenAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            ApplicationView view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
            }
            else
            {
                view.TryEnterFullScreenMode();
            }

            e.Handled = true;
        }

        private void SidebarControl_Loaded(object sender, RoutedEventArgs e)
        {
            SidebarAdaptiveViewModel.UpdateTabControlMargin(); // Set the correct tab margin on startup
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LoadPreviewPaneChanged();
        }

        /// <summary>
        /// Call this function to update the positioning of the preview pane.
        /// This is a workaround as the VisualStateManager causes problems.
        /// </summary>
        private void UpdatePositioning()
        {
            if (!LoadPreviewPane || PreviewPane is null || PreviewPane is null)
            {
                PreviewPaneRow.MinHeight = 0;
                PreviewPaneRow.Height = new GridLength(0);
                PreviewPaneColumn.MinWidth = 0;
                PreviewPaneColumn.Width = new GridLength(0);
            }
            else if (RootGrid.ActualWidth > 700)
            {
                PreviewPane.SetValue(Grid.RowProperty, 1);
                PreviewPane.SetValue(Grid.ColumnProperty, 2);

                PreviewPaneGridSplitter.SetValue(Grid.RowProperty, 1);
                PreviewPaneGridSplitter.SetValue(Grid.ColumnProperty, 1);
                PreviewPaneGridSplitter.Width = 2;
                PreviewPaneGridSplitter.Height = RootGrid.ActualHeight;

                PreviewPaneRow.MinHeight = 0;
                PreviewPaneRow.Height = new GridLength(0);
                PreviewPaneColumn.MinWidth = 150;
                PreviewPaneColumn.Width = AppSettings.PreviewPaneSizeVertical;

                PreviewPane.IsHorizontal = false;
            }
            else if (RootGrid.ActualWidth <= 700)
            {
                PreviewPaneRow.MinHeight = 140;
                PreviewPaneRow.Height = AppSettings.PreviewPaneSizeHorizontal;
                PreviewPaneColumn.MinWidth = 0;
                PreviewPaneColumn.Width = new GridLength(0);

                PreviewPane.SetValue(Grid.RowProperty, 3);
                PreviewPane.SetValue(Grid.ColumnProperty, 0);

                PreviewPaneGridSplitter.SetValue(Grid.RowProperty, 2);
                PreviewPaneGridSplitter.SetValue(Grid.ColumnProperty, 0);
                PreviewPaneGridSplitter.Height = 2;
                PreviewPaneGridSplitter.Width = RootGrid.Width;

                PreviewPane.IsHorizontal = true;
            }
        }

        private void PreviewPaneGridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (PreviewPane == null)
            {
                return;
            }

            if (PreviewPane.IsHorizontal)
            {
                AppSettings.PreviewPaneSizeHorizontal = new GridLength(PreviewPane.ActualHeight);
            }
            else
            {
                AppSettings.PreviewPaneSizeVertical = new GridLength(PreviewPane.ActualWidth);
            }
        }

        public bool LoadPreviewPane => App.AppSettings.PreviewPaneEnabled && !IsPreviewPaneDisabled;

        public bool IsPreviewPaneDisabled => (!(SidebarAdaptiveViewModel.PaneHolder?.ActivePane.InstanceViewModel.IsPageTypeNotHome ?? false) && !(SidebarAdaptiveViewModel.PaneHolder?.IsMultiPaneActive ?? false)) // hide the preview pane when on home page unless multi pane is in use
            || Window.Current.Bounds.Width <= 450 || Window.Current.Bounds.Height <= 400; // Disable the preview pane for small windows as it won't function properly

        private void LoadPreviewPaneChanged()
        {
            NotifyPropertyChanged(nameof(LoadPreviewPane));
            NotifyPropertyChanged(nameof(IsPreviewPaneDisabled));
            UpdatePositioning();
        }

        private void PreviewPane_Loading(FrameworkElement sender, object args)
        {
            UpdatePreviewPaneProperties();
            UpdatePositioning();
            PreviewPane?.Model?.UpdateSelectedItemPreview();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ToggleCompactOverlay() => SetCompactOverlay(ApplicationView.GetForCurrentView().ViewMode != ApplicationViewMode.CompactOverlay);

        private async void SetCompactOverlay(bool isCompact)
        {
            var view = ApplicationView.GetForCurrentView();

            if (!isCompact)
            {
                IsCompactOverlay = !await view.TryEnterViewModeAsync(ApplicationViewMode.Default);
            }
            else
            {
                IsCompactOverlay = await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                view.TryResizeView(new Windows.Foundation.Size(400, 350));
            }
        }

        private bool isCompactOverlay;

        public bool IsCompactOverlay
        {
            get => isCompactOverlay;
            set
            {
                if (value != isCompactOverlay)
                {
                    isCompactOverlay = value;
                    NotifyPropertyChanged(nameof(IsCompactOverlay));
                }
            }
        }

        private void RootGrid_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // prevents the arrow key events from navigating the list instead of switching compact overlay
            if (EnterCompactOverlayKeyboardAccelerator.CheckIsPressed() || ExitCompactOverlayKeyboardAccelerator.CheckIsPressed())
            {
                Focus(FocusState.Keyboard);
            }
        }

        private void NavToolbar_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateNavToolbarProperties();
        }
    }
}
