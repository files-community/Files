using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.SettingsInterfaces;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels.Widgets.Bundles
{
    public class BundleItemViewModel : ObservableObject, IDisposable
    {
        private ImageSource icon;

        public BundleItemViewModel(string path, FilesystemItemType targetType)
        {
            this.Path = path;
            this.TargetType = targetType;

            // Create commands
            OpenInNewTabCommand = new RelayCommand(OpenInNewTab);
            OpenInNewPaneCommand = new RelayCommand(OpenInNewPane);
            OpenItemLocationCommand = new RelayCommand(OpenItemLocation);
            RemoveItemCommand = new RelayCommand(RemoveItem);
        }

        private IBundlesSettings BundlesSettings => App.BundlesSettings;

        public SvgImageSource FolderIcon { get; } = new SvgImageSource()
        {
            RasterizePixelHeight = 128,
            RasterizePixelWidth = 128,
            UriSource = new Uri("ms-appx:///Assets/FolderIcon.svg"),
        };

        public ImageSource Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        public Func<string, uint, (byte[] IconData, byte[] OverlayData, bool IsCustom)> LoadIconOverlay { get; set; }

        public string Name
        {
            get
            {
                string fileName = System.IO.Path.GetFileName(this.Path);

                if (fileName.EndsWith(".lnk"))
                {
                    fileName = fileName.Remove(fileName.Length - 4);
                }

                return fileName;
            }
        }

        public Action<BundleItemViewModel> NotifyItemRemoved { get; set; }
        public ICommand OpenInNewPaneCommand { get; private set; }

        public Visibility OpenInNewPaneVisibility
        {
            get => App.AppSettings.IsDualPaneEnabled && TargetType == FilesystemItemType.Directory ? Visibility.Visible : Visibility.Collapsed;
        }

        public ICommand OpenInNewTabCommand { get; private set; }

        public Visibility OpenInNewTabVisibility
        {
            get => TargetType == FilesystemItemType.Directory ? Visibility.Visible : Visibility.Collapsed;
        }

        public ICommand OpenItemLocationCommand { get; private set; }
        public Action<string, FilesystemItemType, bool, bool, IEnumerable<string>> OpenPath { get; set; }

        public Action<string> OpenPathInNewPane { get; set; }

        /// <summary>
        /// The name of a bundle this item is contained within
        /// </summary>
        public string ParentBundleName { get; set; }

        public string Path { get; set; }
        public ICommand RemoveItemCommand { get; private set; }
        public FilesystemItemType TargetType { get; set; } = FilesystemItemType.File;

        public void Dispose()
        {
            Path = null;
            Icon = null;

            OpenInNewTabCommand = null;
            OpenItemLocationCommand = null;
            RemoveItemCommand = null;

            OpenPath = null;
            OpenPathInNewPane = null;
            LoadIconOverlay = null;
        }

        public void OpenItem()
        {
            OpenPath(Path, TargetType, false, false, null);
        }

        public void RemoveItem()
        {
            if (BundlesSettings.SavedBundles.ContainsKey(ParentBundleName))
            {
                Dictionary<string, List<string>> allBundles = BundlesSettings.SavedBundles;
                allBundles[ParentBundleName].Remove(Path);
                BundlesSettings.SavedBundles = allBundles;
                NotifyItemRemoved(this);
            }
        }

        public async void UpdateIcon()
        {
            if (TargetType == FilesystemItemType.Directory) // OpenDirectory
            {
                Icon = FolderIcon;
            }
            else // NotADirectory
            {
                try
                {
                    if (Path.EndsWith(".lnk"))
                    {
                        var (IconData, OverlayData, IsCustom) = LoadIconOverlay(Path, 24u);

                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                        {
                            Icon = await IconData.ToBitmapAsync();
                        });

                        return;
                    }

                    StorageFile file = await StorageItemHelpers.ToStorageItem<StorageFile>(Path);

                    if (file == null) // No file found
                    {
                        Icon = new BitmapImage();
                        return;
                    }

                    BitmapImage icon = new BitmapImage();
                    StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.ListView, 24u, ThumbnailOptions.UseCurrentScale);

                    if (thumbnail != null)
                    {
                        await icon.SetSourceAsync(thumbnail);

                        Icon = icon;
                        OnPropertyChanged(nameof(Icon));
                    }
                }
                catch
                {
                    Icon = new BitmapImage(); // Set here no file image
                }
            }
        }

        private void OpenInNewPane()
        {
            OpenPathInNewPane(Path);
        }

        private async void OpenInNewTab()
        {
            await MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), Path);
        }

        private void OpenItemLocation()
        {
            OpenPath(System.IO.Path.GetDirectoryName(Path), FilesystemItemType.Directory, false, false, System.IO.Path.GetFileName(Path).CreateEnumerable());
        }
    }
}