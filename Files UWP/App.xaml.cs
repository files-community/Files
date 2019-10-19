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

namespace Files
{
    sealed partial class App : Application
    {
        public static ProHome selectedTabInstance { get; set; }
        public App()
        {
            this.InitializeComponent();
            exceptionDialog = new Dialogs.ExceptionDialog();
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
                    //Debug.WriteLine("Theme Requested as Light");
                }
                else if (localSettings.Values["theme"].ToString() == "Dark")
                {
                    SettingsPages.Personalization.TV.ThemeValue = ApplicationTheme.Dark;
                    //Debug.WriteLine("Theme Requested as Dark");
                }
                else
                {
                    var uiSettings = new Windows.UI.ViewManagement.UISettings();
                    var color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
                    if (color == Colors.White)
                    {
                        SettingsPages.Personalization.TV.ThemeValue = ApplicationTheme.Light;
                       // Debug.WriteLine("Theme Requested as Default (Light)");

                    }
                    else
                    {
                        SettingsPages.Personalization.TV.ThemeValue = ApplicationTheme.Dark;
                        //Debug.WriteLine("Theme Requested as Default (Dark)");
                    }
                }
            }

            this.RequestedTheme = SettingsPages.Personalization.TV.ThemeValue;
            //Debug.WriteLine("!!Requested Theme!!" + RequestedTheme.ToString());

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

            FindDrives();
            //DeviceWatcher watcher = DeviceInformation.CreateWatcher();
            //watcher.Added += (sender, info) => FindDrives();
            //watcher.Removed += (sender, info) => FindDrives();
            //watcher.Start();
        }

        private async void FindDrives()
        {
            foundDrives.Clear();
            var knownRemDevices = new ObservableCollection<string>();
            foreach (var f in await KnownFolders.RemovableDevices.GetFoldersAsync())
            {
                var path = f.Path;
                knownRemDevices.Add(path);
            }

            var driveLetters = DriveInfo.GetDrives().Select(x => x.RootDirectory.Root).ToList().OrderBy(x => x.Root.FullName).ToList();

            if (!driveLetters.Any()) return;

            driveLetters.ForEach(async roots =>
            {
                try
                {
                    //if (roots.Name == @"C:\") return;
                    var content = string.Empty;
                    string icon;
                    if (knownRemDevices.Contains(roots.Name))
                    {
                        content = $"Removable Drive ({roots.Name})";
                        icon = "\uE88E";
                    }
                    else
                    {
                        content = $"Local Disk ({roots.Name})";
                        icon = "\uEDA2";
                    }
                    StorageFolder drive = await StorageFolder.GetFolderFromPathAsync(roots.Name);
                    var retrivedProperties = await drive.Properties.RetrievePropertiesAsync(new string[] { "System.FreeSpace", "System.Capacity" });
                    
                    ulong totalSpaceProg = 0;
                    ulong freeSpaceProg = 0;
                    string free_space_text = "Unknown";
                    string total_space_text = "Unknown";
                    Visibility capacityBarVis = Visibility.Visible;
                    try
                    {
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

                    foundDrives.Add(new DriveItem()
                    {
                        driveText = content,
                        glyph = icon,
                        maxSpace = totalSpaceProg,
                        spaceUsed = totalSpaceProg - freeSpaceProg,
                        tag = roots.Name,
                        progressBarVisibility = capacityBarVis,
                        spaceText = free_space_text + " free of " + total_space_text,
                    });
                }
                catch (UnauthorizedAccessException e)
                {
                    Debug.WriteLine(e.Message);
                }
            });
        }

        public static Windows.UI.Xaml.UnhandledExceptionEventArgs exceptionInfo { get; set; }
        public static string exceptionStackTrace { get; set; }
        public Dialogs.ExceptionDialog exceptionDialog;

        private async void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            if(exceptionDialog.Visibility == Visibility.Visible)
                exceptionDialog.Hide();

            exceptionInfo = e;
            exceptionStackTrace = e.Exception.StackTrace;
            await exceptionDialog.ShowAsync();

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
            deferral.Complete();
        }
    }
}
