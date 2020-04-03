using Files.DataModels;
using Files.Enums;
using Files.Filesystem;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Files.View_Models
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ApplicationDataContainer _roamingSettings;

        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public DrivesManager DrivesManager { get; }

        public SettingsViewModel()
        {
            _roamingSettings = ApplicationData.Current.RoamingSettings;

            DetectApplicationTheme();
            DetectOneDrivePreference();
            DetectDateTimeFormat();
            PinSidebarLocationItems();
            DetectQuickLook();

            DrivesManager = new DrivesManager();

            foundDrives = DrivesManager.Drives;
            //DetectWSLDistros();
            LoadTerminalApps();
        }

        public async void DetectQuickLook()
        {
            // Detect QuickLook
            localSettings.Values["Arguments"] = "StartupTasks";
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        private void PinSidebarLocationItems()
        {
            AddDefaultLocations();
            PopulatePinnedSidebarItems();
        }
        private void AddDefaultLocations()
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
            App.sideBarItems.Add(new LocationItem { Text = resourceLoader.GetString("SidebarHome"), Glyph = "\uE737", IsDefaultLocation = true, Path = "Home" });
            App.sideBarItems.Add(new LocationItem { Text = resourceLoader.GetString("SidebarDesktop"), Glyph = "\uE8FC", IsDefaultLocation = true, Path = DesktopPath });
            App.sideBarItems.Add(new LocationItem { Text = resourceLoader.GetString("SidebarDownloads"), Glyph = "\uE896", IsDefaultLocation = true, Path = DownloadsPath });
            App.sideBarItems.Add(new LocationItem { Text = resourceLoader.GetString("SidebarDocuments"), Glyph = "\uE8A5", IsDefaultLocation = true, Path = DocumentsPath });
            App.sideBarItems.Add(new LocationItem { Text = resourceLoader.GetString("SidebarPictures"), Glyph = "\uEB9F", IsDefaultLocation = true, Path = PicturesPath });
            App.sideBarItems.Add(new LocationItem { Text = resourceLoader.GetString("SidebarMusic"), Glyph = "\uEC4F", IsDefaultLocation = true, Path = MusicPath });
            App.sideBarItems.Add(new LocationItem { Text = resourceLoader.GetString("SidebarVideos"), Glyph = "\uE8B2", IsDefaultLocation = true, Path = VideosPath });
        }

        public List<string> LinesToRemoveFromFile = new List<string>();

        private async void PopulatePinnedSidebarItems()
        {
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
                        foreach (INavigationControlItem sbi in App.sideBarItems)
                        {
                            if (sbi is LocationItem)
                            {
                                if (!string.IsNullOrWhiteSpace(sbi.Path) && !(sbi as LocationItem).IsDefaultLocation)
                                {
                                    if (sbi.Path.ToString() == locationPath)
                                    {
                                        isDuplicate = true;

                                    }
                                }
                            }

                        }

                        if (!isDuplicate)
                        {
                            int insertIndex = App.sideBarItems.IndexOf(App.sideBarItems.Last(x => x.ItemType == NavigationControlItemType.Location)) + 1;
                            App.sideBarItems.Insert(insertIndex, new LocationItem() { IsDefaultLocation = false, Text = name, Glyph = icon, Path = locationPath });
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

        private void RemoveAllSidebarItems(NavigationControlItemType type)
        {
            var itemsOfType = App.sideBarItems.TakeWhile(x => x.ItemType == type);
            foreach (var item in itemsOfType)
            {
                App.sideBarItems.Remove(item);
            }
        }

        public async void RemoveStaleSidebarItems()
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
                var sideBarItems_Copy = App.sideBarItems.ToList();
                foreach (INavigationControlItem location in App.sideBarItems)
                {
                    if (location is LocationItem)
                    {
                        if (!(location as LocationItem).IsDefaultLocation)
                        {
                            if (!ListFileLines.Contains(location.Path.ToString()))
                            {
                                sideBarItems_Copy.Remove(location);
                            }
                        }
                    }
                }
                App.sideBarItems.Clear();
                foreach (INavigationControlItem correctItem in sideBarItems_Copy)
                {
                    App.sideBarItems.Add(correctItem);
                }
                LinesToRemoveFromFile.Clear();
            }
        }

        private async void DetectWSLDistros()
        {
            try
            {
                var distroFolder = await StorageFolder.GetFolderFromPathAsync(@"\\wsl$\");
                if ((await distroFolder.GetFoldersAsync()).Count > 0)
                {
                    AreLinuxFilesSupported = false;
                }

                foreach (StorageFolder folder in await distroFolder.GetFoldersAsync())
                {
                    Uri logoURI = null;
                    if (folder.DisplayName.Contains("ubuntu", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/ubuntupng.png");
                    }
                    else if (folder.DisplayName.Contains("kali", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/kalipng.png");
                    }
                    else if (folder.DisplayName.Contains("debian", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/debianpng.png");
                    }
                    else if (folder.DisplayName.Contains("opensuse", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/opensusepng.png");
                    }
                    else if (folder.DisplayName.Contains("alpine", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/alpinepng.png");
                    }
                    else
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/genericpng.png");
                    }


                    App.sideBarItems.Add(new WSLDistroItem() { DistroName = folder.DisplayName, Path = folder.Path, Logo = logoURI });
                }
            }
            catch (Exception)
            {
                // WSL Not Supported/Enabled
                AreLinuxFilesSupported = false;
            }
        }

        private void DetectDateTimeFormat()
        {
            if (localSettings.Values[LocalSettings.DateTimeFormat] != null)
            {
                if (localSettings.Values[LocalSettings.DateTimeFormat].ToString() == "Application")
                {
                    DisplayedTimeStyle = TimeStyle.Application;
                }
                else if (localSettings.Values[LocalSettings.DateTimeFormat].ToString() == "System")
                {
                    DisplayedTimeStyle = TimeStyle.System;
                }
            }
            else
            {
                localSettings.Values[LocalSettings.DateTimeFormat] = "Application";
            }
        }

        private TimeStyle _DisplayedTimeStyle = TimeStyle.Application;
        public TimeStyle DisplayedTimeStyle
        {
            get => _DisplayedTimeStyle;
            set
            {
                Set(ref _DisplayedTimeStyle, value);
                if (value.Equals(TimeStyle.Application))
                {
                    localSettings.Values[LocalSettings.DateTimeFormat] = "Application";
                }
                else if (value.Equals(TimeStyle.System))
                {
                    localSettings.Values[LocalSettings.DateTimeFormat] = "System";
                }
            }
        }
        private void DetectApplicationTheme()
        {
            if (localSettings.Values["theme"] != null)
            {
                if (localSettings.Values["theme"].ToString() == "Light")
                {
                    ThemeValue = ThemeStyle.Light;
                    App.Current.RequestedTheme = ApplicationTheme.Light;
                    return;
                }
                else if (localSettings.Values["theme"].ToString() == "Dark")
                {
                    ThemeValue = ThemeStyle.Dark;
                    App.Current.RequestedTheme = ApplicationTheme.Dark;
                    return;
                }
            }

            var uiSettings = new UISettings();
            var color = uiSettings.GetColorValue(UIColorType.Background);
            if (color == Colors.White)
            {
                ThemeValue = ThemeStyle.System;
                App.Current.RequestedTheme = ApplicationTheme.Light;
            }
            else
            {
                ThemeValue = ThemeStyle.System;
                App.Current.RequestedTheme = ApplicationTheme.Dark;
            }
        }

        private async void LoadTerminalApps()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var localSettingsFolder = await localFolder.CreateFolderAsync("settings", CreationCollisionOption.OpenIfExists);
            StorageFile file;
            try
            {
                file = await localSettingsFolder.GetFileAsync("terminal.json");
            }
            catch (FileNotFoundException)
            {
                var defaultFile = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/terminal/terminal.json"));

                file = await localSettingsFolder.CreateFileAsync("terminal.json");
                await FileIO.WriteBufferAsync(file, await FileIO.ReadBufferAsync(await defaultFile));
            }

            var content = await FileIO.ReadTextAsync(file);

            var terminals = JsonConvert.DeserializeObject<TerminalFileModel>(content).Terminals;

            Terminals = terminals;
        }

        private IList<TerminalModel> _Terminals = null;
        public IList<TerminalModel> Terminals
        {
            get => _Terminals;
            set => Set(ref _Terminals, value);
        }

        private FormFactorMode _FormFactor = FormFactorMode.Regular;
        public FormFactorMode FormFactor
        {
            get => _FormFactor;
            set => Set(ref _FormFactor, value);
        }

        private ThemeStyle _ThemeValue;
        public ThemeStyle ThemeValue
        {
            get => _ThemeValue;
            set
            {
                Set(ref _ThemeValue, value);
                if (value.Equals(ThemeStyle.System))
                {
                    localSettings.Values["theme"] = "Default";
                }
                else if (value.Equals(ThemeStyle.Light))
                {
                    localSettings.Values["theme"] = "Light";
                }
                else if (value.Equals(ThemeStyle.Dark))
                {
                    localSettings.Values["theme"] = "Dark";
                }
            }
        }

        private void DetectOneDrivePreference()
        {
            if (localSettings.Values["PinOneDrive"] == null) { localSettings.Values["PinOneDrive"] = true; }

            if ((bool)localSettings.Values["PinOneDrive"] == true)
            {
                PinOneDriveToSideBar = true;
            }
            else
            {
                PinOneDriveToSideBar = false;
            }

            try
            {
                StorageFolder.GetFolderFromPathAsync(OneDrivePath);
            }
            catch (Exception)
            {
                PinOneDriveToSideBar = false;
            }
        }

        private bool _PinOneDriveToSideBar = true;
        public bool PinOneDriveToSideBar
        {
            get => _PinOneDriveToSideBar;
            set
            {
                if (value != _PinOneDriveToSideBar)
                {
                    Set(ref _PinOneDriveToSideBar, value);
                    if (value == true)
                    {
                        localSettings.Values["PinOneDrive"] = true;
                        var oneDriveItem = new DriveItem()
                        {
                            driveText = "OneDrive",
                            tag = "OneDrive",
                            cloudGlyphVisibility = Visibility.Visible,
                            driveGlyphVisibility = Visibility.Collapsed,
                            Type = Filesystem.DriveType.VirtualDrive,
                            //itemVisibility = App.AppSettings.PinOneDriveToSideBar
                        };
                        App.sideBarItems.Add(oneDriveItem);
                    }
                    else
                    {
                        localSettings.Values["PinOneDrive"] = false;
                        foreach (INavigationControlItem item in App.sideBarItems.ToList())
                        {
                            if (item is DriveItem && item.ItemType == NavigationControlItemType.OneDrive)
                            {
                                App.sideBarItems.Remove(item);
                            }
                        }
                    }
                }
            }
        }

        private string _TempPath = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Environment", "TEMP", null);
        public string TempPath
        {
            get => _TempPath;
            set => Set(ref _TempPath, value);
        }

        private string _AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public string AppDataPath
        {
            get => _AppDataPath;
            set => Set(ref _AppDataPath, value);
        }

        private string _HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public string HomePath
        {
            get => _HomePath;
            set => Set(ref _HomePath, value);
        }

        private string _WinDirPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        public string WinDirPath
        {
            get => _WinDirPath;
            set => Set(ref _WinDirPath, value);
        }

        public string DesktopPath
        {
            get => Get(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            set => Set(value);
        }

        public string DocumentsPath
        {
            get => Get(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            set => Set(value);
        }

        public string DownloadsPath
        {
            get => Get(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads");
            set => Set(value);
        }

        public string PicturesPath
        {
            get => Get(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            set => Set(value);
        }

        public string MusicPath
        {
            get => Get(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
            set => Set(value);
        }

        public string VideosPath
        {
            get => Get(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
            set => Set(value);
        }

        public string OneDrivePath
        {
            get => Get(Environment.GetEnvironmentVariable("OneDrive"));
            set => Set(value);
        }

        public bool AcrylicSidebar
        {
            get => Get(false);
            set => Set(value);
        }

        public bool ShowFileExtensions
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowConfirmDeleteDialog
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowRibbonContent
        {
            get => Get(true);
            set => Set(value);
        }

        public bool AreLinuxFilesSupported
        {
            get => Get(false);
            set => Set(value);
        }

        public Int32 LayoutMode
        {
            get => Get(0); // List View
            set => Set(value);
        }

        public event EventHandler LayoutModeChangeRequested;

        private RelayCommand toggleLayoutModeGridView;
        public RelayCommand ToggleLayoutModeGridView => toggleLayoutModeGridView = new RelayCommand(() =>
        {
            LayoutMode = 1; // Grid View

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        private RelayCommand toggleLayoutModeListView;
        public RelayCommand ToggleLayoutModeListView => toggleLayoutModeListView = new RelayCommand(() =>
        {
            LayoutMode = 0; // List View

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        [Obsolete]
        public static ObservableCollection<DriveItem> foundDrives = new ObservableCollection<DriveItem>();

        public void Dispose()
        {
            DrivesManager.Dispose();
        }

        public bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = null)
        {
            propertyName = propertyName != null && propertyName.StartsWith("set_", StringComparison.InvariantCultureIgnoreCase)
                ? propertyName.Substring(4)
                : propertyName;

            TValue originalValue = default;

            if (_roamingSettings.Values.ContainsKey(propertyName))
            {
                originalValue = Get(originalValue, propertyName);

                if (!base.Set(ref originalValue, value, propertyName)) return false;
            }

            _roamingSettings.Values[propertyName] = value;

            return true;
        }

        public TValue Get<TValue>(TValue defaultValue, [CallerMemberName] string propertyName = null)
        {
            var name = propertyName ??
                       throw new ArgumentNullException(nameof(propertyName), "Cannot store property of unnamed.");

            name = name.StartsWith("get_", StringComparison.InvariantCultureIgnoreCase)
                ? propertyName.Substring(4)
                : propertyName;

            if (_roamingSettings.Values.ContainsKey(name))
            {
                var value = _roamingSettings.Values[name];

                if (!(value is TValue tValue))
                {
                    if (value is IConvertible)
                    {
                        tValue = (TValue)Convert.ChangeType(value, typeof(TValue));
                    }
                    else
                    {
                        var valueType = value.GetType();
                        var tryParse = typeof(TValue).GetMethod("TryParse", BindingFlags.Instance | BindingFlags.Public);

                        if (tryParse == null) return default;

                        var stringValue = value.ToString();
                        tValue = default;

                        var tryParseDelegate =
                            (TryParseDelegate<TValue>)Delegate.CreateDelegate(valueType, tryParse, false);

                        tValue = (tryParseDelegate?.Invoke(stringValue, out tValue) ?? false) ? tValue : default;
                    }

                    Set(tValue, propertyName); // Put the corrected value in settings.
                    return tValue;
                }

                return tValue;
            }

            return defaultValue;
        }

        delegate bool TryParseDelegate<TValue>(string inValue, out TValue parsedValue);
    }
}