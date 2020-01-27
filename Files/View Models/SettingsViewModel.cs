using Files.Enums;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Filesystem;
using Newtonsoft.Json;
using Files.DataModels;

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
            DetectSidebarOpacity();
            DrivesManager = new DrivesManager();

            foundDrives = DrivesManager.Drives;
            LoadTerminalApps();
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
            // Detect custom locations set from Windows and QuickLook
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
            catch (FileNotFoundException ex)
            {
                var defaultFile = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/terminal/terminal.json"));

                file = await localSettingsFolder.CreateFileAsync("terminal.json");
                await FileIO.WriteBufferAsync(file, await FileIO.ReadBufferAsync(await defaultFile));
            }

            var content = await FileIO.ReadTextAsync(file);

            var terminals = JsonConvert.DeserializeObject<TerminalFileModel>(content).Terminals;

            Terminals = terminals;
        }

        private FormFactorMode _FormFactor = FormFactorMode.Regular;
        private ThemeStyle _ThemeValue;
        private bool _AreLinuxFilesSupported = false;
        private string _DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        private string _DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string _DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        private string _OneDrivePath = Environment.GetEnvironmentVariable("OneDrive");
        private string _PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        private string _MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        private string _VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        private SidebarOpacity _SidebarThemeMode = SidebarOpacity.Opaque;
        private TimeStyle _DisplayedTimeStyle = TimeStyle.Application;
        private IList<TerminalModel> _Terminals = null;

        public IList<TerminalModel> Terminals
        {
            get => _Terminals;
            set => Set(ref _Terminals, value);
        }

        public FormFactorMode FormFactor
        {
            get => _FormFactor;
            set => Set(ref _FormFactor, value);
        }

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

        public bool AreLinuxFilesSupported
        {
            get => _AreLinuxFilesSupported;
            set => Set(ref _AreLinuxFilesSupported, value);
        }

        public string DesktopPath
        {
            get => _DesktopPath;
            set => Set(ref _DesktopPath, value);
        }

        public string DocumentsPath
        {
            get => _DocumentsPath;
            set => Set(ref _DocumentsPath, value);
        }

        public string DownloadsPath
        {
            get => _DownloadsPath;
            set => Set(ref _DownloadsPath, value);
        }

        public string OneDrivePath
        {
            get => _OneDrivePath;
            set => Set(ref _OneDrivePath, value);
        }

        public string PicturesPath
        {
            get => _PicturesPath;
            set => Set(ref _PicturesPath, value);
        }

        public string MusicPath
        {
            get => _MusicPath;
            set => Set(ref _MusicPath, value);
        }

        public string VideosPath
        {
            get => _VideosPath;
            set => Set(ref _VideosPath, value);
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
