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
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Files.View_Models
{
    public class SettingsViewModel : ViewModelBase
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public DrivesManager DrivesManager { get; }

        public SettingsViewModel()
        {
            DetectCustomLocations();
            DetectApplicationTheme();
            DetectDateTimeFormat();
            DetectStorageItemPreferences();
            DetectSidebarOpacity();
            PinSidebarLocationItems();
            DetectOneDrivePreference();
            DetectConfirmDeletePreference();
            DrivesManager = new DrivesManager();

            foundDrives = DrivesManager.Drives;
            //DetectWSLDistros();
            LoadTerminalApps();
        }

        private void DetectConfirmDeletePreference()
        {
            if (localSettings.Values["ShowConfirmDeleteDialog"] == null) { localSettings.Values["ShowConfirmDeleteDialog"] = true; }

            if ((bool)localSettings.Values["ShowConfirmDeleteDialog"] == true)
            {
                ShowConfirmDeleteDialog = true;
            }
            else
            {
                ShowConfirmDeleteDialog = false;
            }
        }

        private void DetectStorageItemPreferences()
        {
            if (localSettings.Values["ShowFileExtensions"] == null) { ShowFileExtensions = true; }

            if ((bool)localSettings.Values["ShowFileExtensions"] == true)
            {
                ShowFileExtensions = true;
            }
            else
            {
                ShowFileExtensions = false;
            }
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

        private void DetectSidebarOpacity()
        {
            if (localSettings.Values["acrylicSidebar"] != null)
            {
                switch (localSettings.Values["acrylicSidebar"])
                {
                    case true:
                        SidebarThemeMode = SidebarOpacity.AcrylicEnabled;
                        break;
                    case false:
                        SidebarThemeMode = SidebarOpacity.Opaque;
                        break;
                }
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

        private async void DetectCustomLocations()
        {
            // Detect custom locations set from Windows and detect QuickLook
            localSettings.Values["Arguments"] = "StartupTasks";
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();

            DesktopPath = localSettings.Values["DetectedDesktopLocation"] as string;
            DownloadsPath = localSettings.Values["DetectedDownloadsLocation"] as string;
            DocumentsPath = localSettings.Values["DetectedDocumentsLocation"] as string;
            PicturesPath = localSettings.Values["DetectedPicturesLocation"] as string;
            MusicPath = localSettings.Values["DetectedMusicLocation"] as string;
            VideosPath = localSettings.Values["DetectedVideosLocation"] as string;
            OneDrivePath = localSettings.Values["DetectedOneDriveLocation"] as string;

            // Overwrite paths for common locations if Custom Locations setting is enabled
            if (localSettings.Values["customLocationsSetting"] != null)
            {
                if (localSettings.Values["customLocationsSetting"].Equals(true))
                {
                    DesktopPath = localSettings.Values["DesktopLocation"] as string;
                    DownloadsPath = localSettings.Values["DownloadsLocation"] as string;
                    DocumentsPath = localSettings.Values["DocumentsLocation"] as string;
                    PicturesPath = localSettings.Values["PicturesLocation"] as string;
                    MusicPath = localSettings.Values["MusicLocation"] as string;
                    VideosPath = localSettings.Values["VideosLocation"] as string;
                    OneDrivePath = localSettings.Values["DetectedOneDriveLocation"] as string;
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

        private SidebarOpacity _SidebarThemeMode = SidebarOpacity.Opaque;

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

        private bool _AreLinuxFilesSupported = false;
        public bool AreLinuxFilesSupported
        {
            get => _AreLinuxFilesSupported;
            set => Set(ref _AreLinuxFilesSupported, value);
        }

        private bool _ShowFileExtensions = true;
        public bool ShowFileExtensions
        {
            get => _ShowFileExtensions;
            set
            {
                if (localSettings.Values["ShowFileExtensions"] == null)
                {
                    localSettings.Values["ShowFileExtensions"] = value;
                }
                else
                {
                    if (value != _ShowFileExtensions)
                    {
                        Set(ref _ShowFileExtensions, value);
                        if (value == true)
                        {
                            localSettings.Values["ShowFileExtensions"] = true;
                        }
                        else
                        {
                            localSettings.Values["ShowFileExtensions"] = false;
                        }
                    }
                }
            }
        }

        private bool _ShowConfirmDeleteDialog = true;
        public bool ShowConfirmDeleteDialog
        {
            get => _ShowConfirmDeleteDialog;
            set
            {
                if (localSettings.Values["ShowConfirmDeleteDialog"] == null)
                {
                    localSettings.Values["ShowConfirmDeleteDialog"] = value;
                }
                else
                {
                    if (value != _ShowConfirmDeleteDialog)
                    {
                        Set(ref _ShowConfirmDeleteDialog, value);
                        if (value == true)
                        {
                            localSettings.Values["ShowConfirmDeleteDialog"] = true;
                        }
                        else
                        {
                            localSettings.Values["ShowConfirmDeleteDialog"] = false;
                        }
                    }
                }
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

        private string _DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        public string DesktopPath
        {
            get => _DesktopPath;
            set => Set(ref _DesktopPath, value);
        }

        private string _DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public string DocumentsPath
        {
            get => _DocumentsPath;
            set => Set(ref _DocumentsPath, value);
        }

        private string _DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        public string DownloadsPath
        {
            get => _DownloadsPath;
            set => Set(ref _DownloadsPath, value);
        }

        private string _OneDrivePath = Environment.GetEnvironmentVariable("OneDrive");
        public string OneDrivePath
        {
            get => _OneDrivePath;
            set => Set(ref _OneDrivePath, value);
        }

        private string _PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public string PicturesPath
        {
            get => _PicturesPath;
            set => Set(ref _PicturesPath, value);
        }

        private string _MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public string MusicPath
        {
            get => _MusicPath;
            set => Set(ref _MusicPath, value);
        }

        private string _VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        public string VideosPath
        {
            get => _VideosPath;
            set => Set(ref _VideosPath, value);
        }

        private bool _ShowRibbonContent = true;
        public bool ShowRibbonContent
        {
            get => _ShowRibbonContent;
            set
            {
                if (localSettings.Values["ShowRibbonContent"] == null)
                {
                    localSettings.Values["ShowRibbonContent"] = value;
                }
                else
                {
                    if (value != _ShowRibbonContent)
                    {
                        Set(ref _ShowRibbonContent, value);
                        if (value == true)
                        {
                            localSettings.Values["ShowRibbonContent"] = true;
                        }
                        else
                        {
                            localSettings.Values["ShowRibbonContent"] = false;
                        }
                    }
                }
            }
        }

        private string _ToggleLayoutModeIcon = ""; // List View
        public string ToggleLayoutModeIcon
        {
            get => _ToggleLayoutModeIcon;
            set => Set(ref _ToggleLayoutModeIcon, value);
        }

        private Int16 _LayoutMode = 0; // List View
        public Int16 LayoutMode
        {
            get => _LayoutMode;
            set => Set(ref _LayoutMode, value);
        }

        private RelayCommand toggleLayoutMode;
        public RelayCommand ToggleLayoutMode => toggleLayoutMode = new RelayCommand(() =>
        {
            if (LayoutMode == 0) // List View
            {
                LayoutMode = 1; // Grid View
            }
            else
            {
                LayoutMode = 0; // List View
            }

            UpdateToggleLayouModeIcon();
        });

        public void UpdateToggleLayouModeIcon()
        {
            if (LayoutMode == 0) // List View
            {
                ToggleLayoutModeIcon = ""; // List View;
            }
            else // List View
            {
                ToggleLayoutModeIcon = ""; // Grid View
            }
        }

        public SidebarOpacity SidebarThemeMode
        {
            get => _SidebarThemeMode;
            set
            {
                Set(ref _SidebarThemeMode, value);
                if (value.Equals(SidebarOpacity.Opaque))
                {
                    localSettings.Values["acrylicSidebar"] = false;
                }
                else
                {
                    localSettings.Values["acrylicSidebar"] = true;
                }
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

        [Obsolete]
        public static ObservableCollection<DriveItem> foundDrives = new ObservableCollection<DriveItem>();

        public void Dispose()
        {
            DrivesManager.Dispose();
        }
    }
}