using Files.Interacts;
using Files.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Windows.UI.Xaml.Media;
using Files.Filesystem;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using Windows.Devices.Enumeration;
using System.Text.RegularExpressions;
using Windows.Devices.Portable;
using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Storage.Search;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls.Primitives;

namespace Files
{
    sealed partial class App : Application
    {
        public static ProHome selectedTabInstance { get; set; }
        DeviceWatcher watcher;
        public static ObservableCollection<SidebarItem> sideBarItems = new ObservableCollection<SidebarItem>();

        public App()
        {
            this.InitializeComponent();
            exceptionDialog = new Dialogs.ExceptionDialog();
            consentDialog = new Dialogs.ConsentDialog();
            this.Suspending += OnSuspending;
            this.UnhandledException += App_UnhandledException;

            AppCenter.Start("682666d1-51d3-4e4a-93d0-d028d43baaa0", typeof(Analytics), typeof(Crashes));

            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (localSettings.Values["theme"] == null)
            {
                localSettings.Values["theme"] = "Default";
            }

            if (localSettings.Values["datetimeformat"] == null)
            {
                localSettings.Values["datetimeformat"] = "Application";
            }

            if (localSettings.Values["theme"] != null)
            {
                if (localSettings.Values["theme"].ToString() == "Light")
                {
                    SettingsPages.Personalization.TV.ThemeValue = ApplicationTheme.Light;
                }
                else if (localSettings.Values["theme"].ToString() == "Dark")
                {
                    SettingsPages.Personalization.TV.ThemeValue = ApplicationTheme.Dark;
                }
                else
                {
                    var uiSettings = new Windows.UI.ViewManagement.UISettings();
                    var color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
                    if (color == Colors.White)
                    {
                        SettingsPages.Personalization.TV.ThemeValue = ApplicationTheme.Light;
                    }
                    else
                    {
                        SettingsPages.Personalization.TV.ThemeValue = ApplicationTheme.Dark;
                    }
                }
            }

            this.RequestedTheme = SettingsPages.Personalization.TV.ThemeValue;

            if (localSettings.Values["FavoritesDisplayed_Start"] == null)
            {
                localSettings.Values["FavoritesDisplayed_Start"] = true;
            }

            if (localSettings.Values["RecentsDisplayed_Start"] == null)
            {
                localSettings.Values["RecentsDisplayed_Start"] = true;
            }

            if (localSettings.Values["DrivesDisplayed_Start"] == null)
            {
                localSettings.Values["DrivesDisplayed_Start"] = false;
            }

            if (localSettings.Values["FavoritesDisplayed_NewTab"] == null)
            {
                localSettings.Values["FavoritesDisplayed_NewTab"] = true;
            }

            if (localSettings.Values["RecentsDisplayed_NewTab"] == null)
            {
                localSettings.Values["RecentsDisplayed_NewTab"] = true;
            }

            if (localSettings.Values["DrivesDisplayed_NewTab"] == null)
            {
                localSettings.Values["DrivesDisplayed_NewTab"] = false;
            }

            PopulatePinnedSidebarItems();
        }

        public void PopulateDrivesListWithLocalDisks()
        {
            var driveLetters = DriveInfo.GetDrives().Select(x => x.RootDirectory.Root).ToList().OrderBy(x => x.Root.FullName).ToList();
            driveLetters.ForEach(async roots =>
            {
                try
                {
                    var content = string.Empty;
                    string icon = null;
                    if (!(await KnownFolders.RemovableDevices.GetFoldersAsync()).Select(x => x.Path).ToList().Contains(roots.Name))
                    {
                        // TODO: Display Custom Names for Local Disks as well
                        if(InstanceTabsView.NormalizePath(roots.Name) != InstanceTabsView.NormalizePath("A:") 
                            && InstanceTabsView.NormalizePath(roots.Name) != InstanceTabsView.NormalizePath("B:"))
                        {
                            content = $"Local Disk ({roots.Name.TrimEnd('\\')})";
                            icon = "\uEDA2";
                        }
                        else
                        {
                            content = $"Floppy Disk ({roots.Name.TrimEnd('\\')})";
                            icon = "\uE74E";
                        }


                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low,
                        async () =>
                        {
                            Visibility capacityBarVis = Visibility.Visible;
                            ulong totalSpaceProg = 0;
                            ulong freeSpaceProg = 0;
                            string free_space_text = "Unknown";
                            string total_space_text = "Unknown";

                            try
                            {
                                StorageFolder drive = await StorageFolder.GetFolderFromPathAsync(roots.Name);
                                var retrivedProperties = await drive.Properties.RetrievePropertiesAsync(new string[] { "System.FreeSpace", "System.Capacity" });

                                var sizeAsGBString = ByteSizeLib.ByteSize.FromBytes((ulong)retrivedProperties["System.FreeSpace"]).GigaBytes;
                                freeSpaceProg = Convert.ToUInt64(sizeAsGBString);

                                sizeAsGBString = ByteSizeLib.ByteSize.FromBytes((ulong)retrivedProperties["System.Capacity"]).GigaBytes;
                                totalSpaceProg = Convert.ToUInt64(sizeAsGBString);

                                free_space_text = ByteSizeLib.ByteSize.FromBytes((ulong)retrivedProperties["System.FreeSpace"]).ToString();
                                total_space_text = ByteSizeLib.ByteSize.FromBytes((ulong)retrivedProperties["System.Capacity"]).ToString();
                            }
                            catch (UnauthorizedAccessException)
                            {
                                capacityBarVis = Visibility.Collapsed;
                            }
                            catch (NullReferenceException)
                            {
                                capacityBarVis = Visibility.Collapsed;
                            }

                            App.foundDrives.Add(new DriveItem()
                            {
                                driveText = content,
                                glyph = icon,
                                maxSpace = totalSpaceProg,
                                spaceUsed = totalSpaceProg - freeSpaceProg,
                                tag = roots.Name,
                                progressBarVisibility = capacityBarVis,
                                spaceText = free_space_text + " free of " + total_space_text,
                            });
                        });
                    }

                }
                catch (UnauthorizedAccessException e)
                {
                    Debug.WriteLine(e.Message);
                }

            });

            

        }

        private async void Watcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            try
            {
                PopulateDrivesListWithLocalDisks();
            }
            catch (UnauthorizedAccessException)
            {
                await consentDialog.ShowAsync();
            }
            DeviceAdded(sender, null);

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low,
            () =>
            {
                App.foundDrives.Add(new DriveItem()
                {
                    driveText = "OneDrive",
                    tag = "OneDrive",
                    cloudGlyphVisibility = Visibility.Visible,
                    driveGlyphVisibility = Visibility.Collapsed
                });
            });
        }

        private void DeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Debug.WriteLine("Devices updated");
        }


        private async void DeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            var devices = DriveInfo.GetDrives().Select(x => x.RootDirectory.Root).ToList().OrderBy(x => x.Root.FullName).ToList();

            foreach (DriveItem driveItem in foundDrives)
            {
                if (!driveItem.tag.Equals("OneDrive"))
                {
                    if (!devices.Any(x => x.Name == driveItem.tag) || devices.Equals(null))
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low,
                        () =>
                        {
                            foundDrives.Remove(driveItem);
                        });
                        return;

                    }
                }
                
            }
        }

        private async void DeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            try
            {
                var devices = (await KnownFolders.RemovableDevices.GetFoldersAsync()).OrderBy(x => x.Path);
                foreach (StorageFolder device in devices)
                {
                    var letter = device.Path;
                    if (!foundDrives.Any(x => x.tag == letter))
                    {
                        var content = device.DisplayName;
                        string icon = null;
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low,
                        async () =>
                        {
                            if (content.Contains("DVD"))
                            {
                                icon = "\uE958";
                            }
                            else
                            {
                                icon = "\uE88E";
                            }

                            ulong totalSpaceProg = 0;
                            ulong freeSpaceProg = 0;
                            string free_space_text = "Unknown";
                            string total_space_text = "Unknown";
                            Visibility capacityBarVis = Visibility.Visible;
                            try
                            {
                                StorageFolder drive = await StorageFolder.GetFolderFromPathAsync(letter);
                                var retrivedProperties = await drive.Properties.RetrievePropertiesAsync(new string[] { "System.FreeSpace", "System.Capacity" });

                                var sizeAsGBString = ByteSizeLib.ByteSize.FromBytes((ulong)retrivedProperties["System.FreeSpace"]).GigaBytes;
                                freeSpaceProg = Convert.ToUInt64(sizeAsGBString);

                                sizeAsGBString = ByteSizeLib.ByteSize.FromBytes((ulong)retrivedProperties["System.Capacity"]).GigaBytes;
                                totalSpaceProg = Convert.ToUInt64(sizeAsGBString);

                                free_space_text = ByteSizeLib.ByteSize.FromBytes((ulong)retrivedProperties["System.FreeSpace"]).ToString();
                                total_space_text = ByteSizeLib.ByteSize.FromBytes((ulong)retrivedProperties["System.Capacity"]).ToString();
                            }
                            catch (UnauthorizedAccessException)
                            {
                                capacityBarVis = Visibility.Collapsed;
                            }
                            catch (NullReferenceException)
                            {
                                capacityBarVis = Visibility.Collapsed;
                            }

                            if (!foundDrives.Any(x => x.tag == letter))
                            {
                                foundDrives.Add(new DriveItem()
                                {
                                    driveText = content,
                                    glyph = icon,
                                    maxSpace = totalSpaceProg,
                                    spaceUsed = totalSpaceProg - freeSpaceProg,
                                    tag = letter,
                                    progressBarVisibility = capacityBarVis,
                                    spaceText = free_space_text + " free of " + total_space_text,
                                });
                            }
                        });

                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                await consentDialog.ShowAsync();
            }
        }

        public static List<string> LinesToRemoveFromFile = new List<string>();

        public async void PopulatePinnedSidebarItems()
        {
            AddDefaultLocations();

            StorageFile ListFile;
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            ListFile = await cacheFolder.CreateFileAsync("PinnedItems.txt", CreationCollisionOption.OpenIfExists);

            if (ListFile != null)
            {
                var ListFileLines = await FileIO.ReadLinesAsync(ListFile);
                foreach (string locationPath in ListFileLines)
                {
                    try
                    {
                        StorageFolder fol = await StorageFolder.GetFolderFromPathAsync(locationPath);
                        var name = fol.DisplayName;
                        var content = name;
                        var icon = "\uE8B7";

                        bool isDuplicate = false;
                        foreach (SidebarItem sbi in sideBarItems)
                        {
                            if (!string.IsNullOrWhiteSpace(sbi.Path) && !sbi.isDefaultLocation)
                            {
                                if (sbi.Path.ToString() == locationPath)
                                {
                                    isDuplicate = true;

                                }
                            }
                        }

                        if (!isDuplicate)
                        {
                            sideBarItems.Add(new SidebarItem() { isDefaultLocation = false, Text = name, IconGlyph = icon, Path = locationPath });
                        }
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    catch (FileNotFoundException e)
                    {
                        Debug.WriteLine("Pinned item was deleted and will be removed from the file lines list soon: " + e.Message);
                        LinesToRemoveFromFile.Add(locationPath);
                    }
                    catch (System.Runtime.InteropServices.COMException e)
                    {
                        Debug.WriteLine("Pinned item's drive was ejected and will be removed from the file lines list soon: " + e.Message);
                        LinesToRemoveFromFile.Add(locationPath);
                    }
                }

                RemoveStaleSidebarItems();
            }
        }

        private void AddDefaultLocations()
        {
            sideBarItems.Add(new SidebarItem() { Text = "Home", IconGlyph = "\uE737", isDefaultLocation = true });
            sideBarItems.Add(new SidebarItem() { Text = "Desktop", IconGlyph = "\uE8FC", isDefaultLocation = true });
            sideBarItems.Add(new SidebarItem() { Text = "Downloads", IconGlyph = "\uE896", isDefaultLocation = true });
            sideBarItems.Add(new SidebarItem() { Text = "Documents", IconGlyph = "\uE8A5", isDefaultLocation = true });
            sideBarItems.Add(new SidebarItem() { Text = "Pictures", IconGlyph = "\uEB9F", isDefaultLocation = true });
            sideBarItems.Add(new SidebarItem() { Text = "Music", IconGlyph = "\uEC4F", isDefaultLocation = true });
            sideBarItems.Add(new SidebarItem() { Text = "Videos", IconGlyph = "\uE8B2", isDefaultLocation = true });
        }

        public static async void RemoveStaleSidebarItems()
        {
            StorageFile ListFile;
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            ListFile = await cacheFolder.CreateFileAsync("PinnedItems.txt", CreationCollisionOption.OpenIfExists);

            if (ListFile != null)
            {
                var ListFileLines = await FileIO.ReadLinesAsync(ListFile);
                foreach (string path in LinesToRemoveFromFile)
                {
                    ListFileLines.Remove(path);
                }

                await FileIO.WriteLinesAsync(ListFile, ListFileLines);
                ListFileLines = await FileIO.ReadLinesAsync(ListFile);

                // Remove unpinned items from sidebar
                var sideBarItems_Copy = sideBarItems.ToList();
                foreach (SidebarItem location in sideBarItems)
                {
                    if(!location.isDefaultLocation)
                    {
                        if (!ListFileLines.Contains(location.Path.ToString()))
                        {
                            sideBarItems_Copy.Remove(location);
                        }
                    }
                    
                }
                sideBarItems.Clear();
                foreach(SidebarItem correctItem in sideBarItems_Copy)
                {
                    sideBarItems.Add(correctItem);
                }
                LinesToRemoveFromFile.Clear();
            }
        }

        public static SidebarItem rightClickedItem;

        public static async void FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            var ListFile = await cacheFolder.GetFileAsync("PinnedItems.txt");
            var ListFileLines = await FileIO.ReadLinesAsync(ListFile);
            foreach (string path in ListFileLines)
            {
                if (path == App.rightClickedItem.Path.ToString())
                {
                    App.LinesToRemoveFromFile.Add(path);
                    RemoveStaleSidebarItems();
                    return;
                }
            }
        }



        public static Windows.UI.Xaml.UnhandledExceptionEventArgs exceptionInfo { get; set; }
        public static string exceptionStackTrace { get; set; }
        public Dialogs.ExceptionDialog exceptionDialog;
        public static Dialogs.ConsentDialog consentDialog { get; set; }

        private async void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            exceptionInfo = e;
            exceptionStackTrace = e.Exception.StackTrace;
            await exceptionDialog.ShowAsync(); 
        }

        public static IReadOnlyList<ContentDialog> FindDisplayedContentDialogs<T>()
        {
            var popupElements = VisualTreeHelper.GetOpenPopupsForXamlRoot(Window.Current.Content.XamlRoot);
            List<ContentDialog> dialogs = new List<ContentDialog>();
            List<ContentDialog> openDialogs = new List<ContentDialog>();
            Interaction.FindChildren<ContentDialog>(dialogs, Window.Current.Content.XamlRoot.Content as DependencyObject);
            foreach(var dialog in dialogs)
            {
                var popups = new List<Popup>();
                Interaction.FindChildren<Popup>(popups, dialog);
                if (popups.First().IsOpen && popups.First() is T)
                {
                    openDialogs.Add(dialog);
                }
            }
            return openDialogs;
        }

        public static PasteState PS { get; set; } = new PasteState();
        public static List<string> pathsToDeleteAfterPaste = new List<string>();
        public static ObservableCollection<DriveItem> foundDrives = new ObservableCollection<DriveItem>();

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            bool canEnablePrelaunch = Windows.Foundation.Metadata.ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "EnablePrelaunch");

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (canEnablePrelaunch)
                {
                    TryEnablePrelaunch();
                }

                if (rootFrame.Content == null)
                {

                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(InstanceTabsView), e.Arguments, new SuppressNavigationTransitionInfo());


                }
                watcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());
                watcher.Added += DeviceAdded;
                watcher.Removed += DeviceRemoved;
                watcher.Updated += DeviceUpdated;
                watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
                watcher.Start();
                // Ensure the current window is active
                Window.Current.Activate();

            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            // Window management
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                Window.Current.Content = rootFrame;
            }

            if (args.Kind == ActivationKind.Protocol)
            {
                var eventArgs = args as ProtocolActivatedEventArgs;

                if (eventArgs.Uri.AbsoluteUri == "files-uwp:")
                {
                    rootFrame.Navigate(typeof(InstanceTabsView), null, new SuppressNavigationTransitionInfo());
                }
                else
                {
                    var trimmedPath = eventArgs.Uri.OriginalString.Split('=')[1];
                    rootFrame.Navigate(typeof(InstanceTabsView), @trimmedPath, new SuppressNavigationTransitionInfo());
                }
                // Ensure the current window is active.
                watcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());
                watcher.Added += DeviceAdded;
                watcher.Removed += DeviceRemoved;
                watcher.Updated += DeviceUpdated;
                watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
                watcher.Start();
                Window.Current.Activate();
                return;
            }

            rootFrame.Navigate(typeof(InstanceTabsView), null, new SuppressNavigationTransitionInfo());

            // Ensure the current window is active.
            Window.Current.Activate();
        }

        private void TryEnablePrelaunch()
        {
            Windows.ApplicationModel.Core.CoreApplication.EnablePrelaunch(true);
        }
        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            watcher.Stop();
            deferral.Complete();
        }
    }
}
