using Files.Interacts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Windows.UI.Core;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.System;
using Files.CommandLine;
using Files.View_Models;
using Files.Controls;

namespace Files
{
    sealed partial class App : Application
    {
        
        private static IShellPage currentInstance;
        public static IShellPage CurrentInstance
        {
            get
            {
                return currentInstance;
            }
            set
            {
                if(value != currentInstance)
                {
                    currentInstance = value; 
                }
            }
        }
        public static Dialogs.ExceptionDialog exceptionDialog { get; set; }
        public static Dialogs.ConsentDialog consentDialog { get; set; }
        public static Dialogs.PropertiesDialog propertiesDialog { get; set; }
        public static Dialogs.LayoutDialog layoutDialog { get; set; }
        public static Dialogs.AddItemDialog addItemDialog { get; set; }
        public static ObservableCollection<INavigationControlItem> sideBarItems = new ObservableCollection<INavigationControlItem>();
        public static ObservableCollection<LocationItem> locationItems = new ObservableCollection<LocationItem>();
        public static ObservableCollection<WSLDistroItem> linuxDistroItems = new ObservableCollection<WSLDistroItem>();
        public static SettingsViewModel AppSettings { get; set; }

        public App()
        {
	        this.InitializeComponent();
            this.Suspending += OnSuspending;
            consentDialog = new Dialogs.ConsentDialog();
            propertiesDialog = new Dialogs.PropertiesDialog();
            layoutDialog = new Dialogs.LayoutDialog();
            addItemDialog = new Dialogs.AddItemDialog();
            exceptionDialog = new Dialogs.ExceptionDialog();
            //this.UnhandledException += App_UnhandledException;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            Clipboard_ContentChanged(null, null);
            AppCenter.Start("682666d1-51d3-4e4a-93d0-d028d43baaa0", typeof(Analytics), typeof(Crashes));

            AppSettings = new SettingsViewModel();
            PopulatePinnedSidebarItems();
            DetectWSLDistros();

        }

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            if (args.CurrentPoint.Properties.IsXButton1Pressed)
            {
                NavigationActions.Back_Click(null, null);
            }
            else if (args.CurrentPoint.Properties.IsXButton1Pressed)
            {
                NavigationActions.Forward_Click(null, null);
            }
        }

        private async void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
            var alt = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu);
            if (App.CurrentInstance != null)
            {
                if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (shift.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        if (App.CurrentInstance.ContentPage != null)
                        {
                            switch (args.VirtualKey)
                            {
                                case VirtualKey.N:
                                    Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
                                    await App.addItemDialog.ShowAsync();
                                    Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (App.CurrentInstance.ContentPage != null)
                        {
                            switch (args.VirtualKey)
                            {
                                case VirtualKey.C:
                                    App.CurrentInstance.InteractionOperations.CopyItem_ClickAsync(null, null);
                                    break;
                                case VirtualKey.X:
                                    App.CurrentInstance.InteractionOperations.CutItem_Click(null, null);
                                    break;
                                case VirtualKey.V:
                                    App.CurrentInstance.InteractionOperations.PasteItem_ClickAsync(null, null);
                                    break;
                                case VirtualKey.A:
                                    App.CurrentInstance.InteractionOperations.SelectAllItems();
                                    break;
                            }
                        }

                        switch (args.VirtualKey)
                        {
                            case VirtualKey.N:
                                var filesUWPUri = new Uri("files-uwp:");
                                await Launcher.LaunchUriAsync(filesUWPUri);
                                break;
                            case VirtualKey.W:
                                if (((Window.Current.Content as Frame).Content as InstanceTabsView).TabStrip.TabItems.Count == 1)
                                {
                                    Application.Current.Exit();
                                }
                                else if (((Window.Current.Content as Frame).Content as InstanceTabsView).TabStrip.TabItems.Count > 1)
                                {
                                    ((Window.Current.Content as Frame).Content as InstanceTabsView).TabStrip.TabItems.RemoveAt(((Window.Current.Content as Frame).Content as InstanceTabsView).TabStrip.SelectedIndex);
                                }
                                break;
                        }
                    }
                }
                else if (ctrl.HasFlag(CoreVirtualKeyStates.None) && alt.HasFlag(CoreVirtualKeyStates.None))
                {
                    if (App.CurrentInstance.ContentPage != null)
                    {
                        switch (args.VirtualKey)
                        {
                            case VirtualKey.Delete:
                                App.CurrentInstance.InteractionOperations.DeleteItem_Click(null, null);
                                break;
                            case VirtualKey.Enter:
                                if ((App.CurrentInstance.ContentPage).IsQuickLookEnabled)
                                {
                                    App.CurrentInstance.InteractionOperations.ToggleQuickLook();
                                }
                                else
                                {
                                    App.CurrentInstance.InteractionOperations.List_ItemClick(null, null);
                                }
                                break;
                        }

                        if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
                        {
                            switch (args.VirtualKey)
                            {
                                case VirtualKey.F2:
                                    if((App.CurrentInstance.ContentPage).SelectedItems.Count > 0)
                                    {
                                        App.CurrentInstance.InteractionOperations.RenameItem_Click(null, null);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private async void DetectWSLDistros()
        {
            try
            {
                var distroFolder = await StorageFolder.GetFolderFromPathAsync(@"\\wsl$\");
                if ((await distroFolder.GetFoldersAsync()).Count > 0)
                {
                    AppSettings.AreLinuxFilesSupported = false;
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


                    linuxDistroItems.Add(new WSLDistroItem() { DistroName = folder.DisplayName, Path = folder.Path, Logo = logoURI });
                }
            }
            catch (Exception)
            {
                // WSL Not Supported/Enabled
                AppSettings.AreLinuxFilesSupported = false;
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
                        foreach (INavigationControlItem sbi in sideBarItems)
                        {
                            if(sbi is LocationItem)
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
                            sideBarItems.Add(new LocationItem() { IsDefaultLocation = false, Text = name, Glyph = icon, Path = locationPath });
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
            sideBarItems.Add(new LocationItem { Text = "Home", Glyph = "\uE737", IsDefaultLocation = true, Path = "Home" });
            sideBarItems.Add(new LocationItem { Text = "Desktop", Glyph = "\uE8FC", IsDefaultLocation = true, Path = AppSettings.DesktopPath });
            sideBarItems.Add(new LocationItem { Text = "Downloads", Glyph = "\uE896", IsDefaultLocation = true, Path = AppSettings.DownloadsPath });
            sideBarItems.Add(new LocationItem { Text = "Documents", Glyph = "\uE8A5", IsDefaultLocation = true, Path = AppSettings.DocumentsPath });
            sideBarItems.Add(new LocationItem { Text = "Pictures", Glyph = "\uEB9F", IsDefaultLocation = true, Path = AppSettings.PicturesPath });
            sideBarItems.Add(new LocationItem { Text = "Music", Glyph = "\uEC4F", IsDefaultLocation = true, Path = AppSettings.MusicPath });
            sideBarItems.Add(new LocationItem { Text = "Videos", Glyph = "\uE8B2", IsDefaultLocation = true, Path = AppSettings.VideosPath });
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
                foreach (INavigationControlItem location in sideBarItems)
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
                sideBarItems.Clear();
                foreach(INavigationControlItem correctItem in sideBarItems_Copy)
                {
                    sideBarItems.Add(correctItem);
                }
                LinesToRemoveFromFile.Clear();
            }
        }

        public static INavigationControlItem rightClickedItem;

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

        public static void Clipboard_ContentChanged(object sender, object e)
        {
            try
            {
                DataPackageView packageView = Clipboard.GetContent();
                if (packageView.Contains(StandardDataFormats.StorageItems) && App.CurrentInstance.CurrentPageType != typeof(YourHome))
                {
                    App.PS.isEnabled = true;
                }
                else
                {
                    App.PS.isEnabled = false;
                }
            }
            catch (Exception)
            {
                App.PS.isEnabled = false;
            }

        }

        public static Windows.UI.Xaml.UnhandledExceptionEventArgs exceptionInfo { get; set; }
        public static string exceptionStackTrace { get; set; }
        


        private async void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            exceptionInfo = e;
            exceptionStackTrace = e.Exception.StackTrace;
            await exceptionDialog.ShowAsync(ContentDialogPlacement.Popup); 
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
                Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
                Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;

            }
        }

        private void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args.KeyStatus.IsMenuKeyDown)
            {
                switch (args.VirtualKey)
                {
                    case VirtualKey.Left:
                        NavigationActions.Back_Click(null, null);
                        break;
                    case VirtualKey.Right:
                        NavigationActions.Forward_Click(null, null);
                        break;
                    case VirtualKey.F:
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 0;
                        break;
                    case VirtualKey.H:
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 1;
                        break;
                    case VirtualKey.S:
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 2;
                        break;
                    case VirtualKey.V:
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 3;
                        break;
                }
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

            switch (args.Kind)
            {
                case ActivationKind.Protocol:
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
                    Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
                    Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                    Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
                    return;

                case ActivationKind.CommandLineLaunch:
                    var cmdLineArgs = args as CommandLineActivatedEventArgs;
                    var operation = cmdLineArgs.Operation;
                    var cmdLineString = operation.Arguments;
                    var activationPath = operation.CurrentDirectoryPath;

                    var parsedCommands = CommandLineParser.ParseUntrustedCommands(cmdLineString);

                    if (parsedCommands != null && parsedCommands.Count > 0)
                    {
                        foreach (var command in parsedCommands)
                        {
                            switch (command.Type)
                            {
                                case ParsedCommandType.OpenDirectory:
                                    // TODO Open Directory

                                    rootFrame.Navigate(typeof(InstanceTabsView), command.Payload, new SuppressNavigationTransitionInfo());

                                    // Ensure the current window is active.
                                    Window.Current.Activate();
                                    Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
                                    Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                                    Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;

                                    return;
                                case ParsedCommandType.Unkwon:
                                    rootFrame.Navigate(typeof(InstanceTabsView), null, new SuppressNavigationTransitionInfo());
                                    // Ensure the current window is active.
                                    Window.Current.Activate();
                                    Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
                                    Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                                    Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
                                    return;
                            }
                        }
                    }
                    break;
            }

            rootFrame.Navigate(typeof(InstanceTabsView), null, new SuppressNavigationTransitionInfo());

            // Ensure the current window is active.
            Window.Current.Activate();
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
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
            AppSettings.Dispose();
            deferral.Complete();
        }
    }

    public class WSLDistroItem : INavigationControlItem
    {
        public string DistroName { get; set; }
        public string Path { get; set; }
        public Uri Logo { get; set; }

        string INavigationControlItem.IconGlyph => null;

        string INavigationControlItem.Text => DistroName;

        string INavigationControlItem.Path => Path;

        NavigationControlItemType INavigationControlItem.ItemType => NavigationControlItemType.LinuxDistro;
    }
}
