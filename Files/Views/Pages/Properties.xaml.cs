using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.View_Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Foundation.Collections;
using FileAttributes = System.IO.FileAttributes;
using static Files.Helpers.NativeFindStorageItemHelper;

namespace Files
{
    public sealed partial class Properties : Page
    {
        private static AppWindowTitleBar TitleBar;

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public AppWindow propWindow;

        public SettingsViewModel AppSettings => App.AppSettings;

        public SelectedItemsPropertiesViewModel ViewModel { get; set; }

        public Properties()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel = new SelectedItemsPropertiesViewModel(e.Parameter as ListedItem);
            ViewModel.ItemMD5HashProgress = ItemMD5HashProgress;
            ViewModel.Dispatcher = Dispatcher;
            AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
            base.OnNavigatedTo(e);
        }

        private async void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                propWindow = Interaction.AppWindows[UIContext]; // Collect AppWindow-specific info

                TitleBar = propWindow.TitleBar; // Set properties window titleBar style
                TitleBar.ButtonBackgroundColor = Colors.Transparent;
                TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                AppSettings.UpdateThemeElements.Execute(null);
            }
            await GetPropertiesAsync(_tokenSource);
        }

        private void Properties_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_tokenSource != null && !_tokenSource.IsCancellationRequested)
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
                _tokenSource = null;
            }
        }

        private void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            RequestedTheme = ThemeHelper.RootTheme;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                switch (ThemeHelper.RootTheme)
                {
                    case ElementTheme.Default:
                        TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                        TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                        break;

                    case ElementTheme.Light:
                        TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                        TitleBar.ButtonForegroundColor = Colors.Black;
                        break;

                    case ElementTheme.Dark:
                        TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                        TitleBar.ButtonForegroundColor = Colors.White;
                        break;
                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await propWindow.CloseAsync();
            }
            else
            {
                App.PropertiesDialogDisplay.Hide();
            }
        }

        public async void GetOtherPropeties(StorageItemContentProperties properties)
        {
            string dateAccessedProperty = "System.DateAccessed";
            string fileOwnerProperty = "System.FileOwner";
            List<string> propertiesName = new List<string>();
            propertiesName.Add(dateAccessedProperty);
            propertiesName.Add(fileOwnerProperty);
            IDictionary<string, object> extraProperties = await properties.RetrievePropertiesAsync(propertiesName);
            ViewModel.ItemAccessedTimestamp = ListedItem.GetFriendlyDate((DateTimeOffset)extraProperties[dateAccessedProperty]);
            ViewModel.ItemFileOwner = extraProperties[fileOwnerProperty].ToString();
        }

        private async void GetFolderSize(StorageFolder storageFolder, CancellationToken token)
        {
            ViewModel.ItemSizeProgressVisibility = Visibility.Visible;

            var fileSizeTask = Task.Run(async () =>
            {
                var size = await CalculateFolderSizeAsync(storageFolder.Path, token);
                return size;
            });
            try
            {
                var folderSize = await fileSizeTask;
                ViewModel.ItemSizeReal = folderSize;
                ViewModel.ItemsSize = ByteSizeLib.ByteSize.FromBytes(folderSize).ToBinaryString().ConvertSizeAbbreviation()
                    + " (" + ByteSizeLib.ByteSize.FromBytes(folderSize).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                ViewModel.SizeCalcError = true;
            }
            ViewModel.ItemSizeProgressVisibility = Visibility.Collapsed;
            SetItemsCountString();
        }

        public async Task<long> CalculateFolderSizeAsync(string path, CancellationToken token)
        {
            long size = 0;

            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

            IntPtr hFile = FindFirstFileExFromApp(path + "\\*.*", findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                                                  additionalFlags);

            var count = 0;
            if (hFile.ToInt64() != -1)
            {
                do
                {
                    if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                    {
                        long fDataFSize = findData.nFileSizeLow;
                        long fileSize;
                        if (fDataFSize < 0 && findData.nFileSizeHigh > 0)
                        {
                            fileSize = fDataFSize + 4294967296 + (findData.nFileSizeHigh * 4294967296);
                        }
                        else
                        {
                            if (findData.nFileSizeHigh > 0)
                            {
                                fileSize = fDataFSize + (findData.nFileSizeHigh * 4294967296);
                            }
                            else if (fDataFSize < 0)
                            {
                                fileSize = fDataFSize + 4294967296;
                            }
                            else
                            {
                                fileSize = fDataFSize;
                            }
                        }
                        size += fileSize;
                        ++count;
                        ViewModel.FilesCount++;
                    }
                    else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (findData.cFileName != "." && findData.cFileName != "..")
                        {
                            var itemPath = Path.Combine(path, findData.cFileName);

                            size += await CalculateFolderSizeAsync(itemPath, token);
                            ++count;
                            ViewModel.FoldersCount++;
                        }
                    }

                    if (size > ViewModel.ItemSizeReal)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            ViewModel.ItemSizeReal = size;
                            ViewModel.ItemsSize = ByteSizeLib.ByteSize.FromBytes(size).ToBinaryString().ConvertSizeAbbreviation();
                            SetItemsCountString();
                        });
                    }

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                } while (FindNextFile(hFile, out findData));
                FindClose(hFile);
                return size;
            }
            else
            {
                return 0;
            }
        }

        public async Task GetPropertiesAsync(CancellationTokenSource _tokenSource)
        {
            if (ViewModel.Item.PrimaryItemAttribute == StorageItemTypes.File)
            {
                var file = await StorageFile.GetFileFromPathAsync(ViewModel.Item.ItemPath);
                ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(file.DateCreated);
                GetOtherPropeties(file.Properties);
                ViewModel.ItemsSize = ByteSizeLib.ByteSize.FromBytes(ViewModel.Item.FileSizeBytes).ToBinaryString().ConvertSizeAbbreviation()
                    + " (" + ByteSizeLib.ByteSize.FromBytes(ViewModel.Item.FileSizeBytes).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";

                // Get file MD5 hash
                var hashAlgTypeName = HashAlgorithmNames.Md5;
                ViewModel.ItemMD5HashProgressVisibility = Visibility.Visible;
                ViewModel.ItemMD5HashVisibility = Visibility.Visible;
                try
                {
                    ViewModel.ItemMD5Hash = await App.CurrentInstance.InteractionOperations.GetHashForFile(ViewModel.Item, hashAlgTypeName, _tokenSource.Token, ItemMD5HashProgress);
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                    ViewModel.ItemMD5HashCalcError = true;
                }
            }
            else if (ViewModel.Item.PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                StorageFolder storageFolder = null;
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    storageFolder = await StorageFolder.GetFolderFromPathAsync(ViewModel.Item.ItemPath);
                    ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(storageFolder.DateCreated);
                    GetOtherPropeties(storageFolder.Properties);
                    GetFolderSize(storageFolder, _tokenSource.Token);
                }
                else
                {
                    var parentDirectory = App.CurrentInstance.FilesystemViewModel.CurrentFolder;
                    if (parentDirectory.ItemPath.StartsWith(AppSettings.RecycleBinPath))
                    {
                        // GetFolderFromPathAsync cannot access recyclebin folder
                        if (App.Connection != null)
                        {
                            var value = new ValueSet();
                            value.Add("Arguments", "RecycleBin");
                            value.Add("action", "Query");
                            // Send request to fulltrust process to get recyclebin properties
                            var response = await App.Connection.SendMessageAsync(value);
                            if (response.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success)
                            {
                                ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(DateTime.FromBinary((long)response.Message["DateCreated"]));
                                ViewModel.ItemSizeReal = (long)response.Message["BinSize"];
                                ViewModel.ItemsSize = ByteSizeLib.ByteSize.FromBytes((long)response.Message["BinSize"]).ToString();
                                ViewModel.ItemAccessedTimestamp = ListedItem.GetFriendlyDate(DateTime.FromBinary((long)response.Message["DateAccessed"]));
                                ViewModel.ItemFileOwner = (string)response.Message["FileOwner"];
                            }
                        }
                    }
                    else
                    {
                        storageFolder = await StorageFolder.GetFolderFromPathAsync(parentDirectory.ItemPath);
                        ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(storageFolder.DateCreated);
                        GetOtherPropeties(storageFolder.Properties);
                        GetFolderSize(storageFolder, _tokenSource.Token);
                    }
                }
            }
        }

        private void SetItemsCountString()
        {
            ViewModel.FilesAndFoldersCountString = string.Format(ResourceController.GetTranslation("PropertiesFilesAndFoldersCountString"), ViewModel.FilesCount, ViewModel.FoldersCount);
            ViewModel.FilesAndFoldersCountVisibility = Visibility.Visible;
        }
    }
}