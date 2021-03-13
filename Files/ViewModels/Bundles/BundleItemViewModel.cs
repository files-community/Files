using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.SettingsInterfaces;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels.Bundles
{
    public class BundleItemViewModel : ObservableObject, IDisposable
    {
        #region Singleton

        private IBundlesSettings BundlesSettings => App.BundlesSettings;

        #endregion Singleton

        #region Private Members

        private IShellPage associatedInstance;

        #endregion Private Members

        #region Actions

        public Action<BundleItemViewModel> NotifyItemRemoved { get; set; }

        #endregion Actions

        #region Public Properties

        /// <summary>
        /// The name of a bundle this item is contained within
        /// </summary>
        public string ParentBundleName { get; set; }

        public string Path { get; set; }

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

        public FilesystemItemType TargetType { get; set; } = FilesystemItemType.File;

        private ImageSource icon;

        public ImageSource Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        public SvgImageSource FolderIcon { get; } = new SvgImageSource()
        {
            RasterizePixelHeight = 128,
            RasterizePixelWidth = 128,
            UriSource = new Uri("ms-appx:///Assets/FolderIcon.svg"),
        };

        public Visibility OpenInNewTabVisibility
        {
            get => TargetType == FilesystemItemType.Directory ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility OpenInNewPaneVisibility
        {
            get => App.AppSettings.IsDualPaneEnabled && TargetType == FilesystemItemType.Directory ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion Public Properties

        #region Commands

        public ICommand OpenInNewTabCommand { get; private set; }

        public ICommand OpenInNewPaneCommand { get; private set; }

        public ICommand OpenItemLocationCommand { get; private set; }

        public ICommand RemoveItemCommand { get; private set; }

        #endregion Commands

        #region Constructor

        public BundleItemViewModel(IShellPage associatedInstance, string path, FilesystemItemType targetType)
        {
            this.associatedInstance = associatedInstance;
            this.Path = path;
            this.TargetType = targetType;

            // Create commands
            OpenInNewTabCommand = new RelayCommand(OpenInNewTab);
            OpenInNewPaneCommand = new RelayCommand(OpenInNewPane);
            OpenItemLocationCommand = new RelayCommand(OpenItemLocation);
            RemoveItemCommand = new RelayCommand(RemoveItem);

            SetIcon();
        }

        #endregion Constructor

        #region Command Implementation

        private async void OpenInNewTab()
        {
            await MainPage.AddNewTabByPathAsync(typeof(PaneHolderPage), Path);
        }

        private void OpenInNewPane()
        {
            associatedInstance.PaneHolder.OpenPathInNewPane(Path);
        }

        private async void OpenItemLocation()
        {
            await associatedInstance.InteractionOperations.OpenPath(System.IO.Path.GetDirectoryName(Path), FilesystemItemType.Directory, selectItems: System.IO.Path.GetFileName(Path).CreateEnumerable());
        }

        #endregion Command Implementation

        #region Private Helpers

        private async void SetIcon()
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
                        var (IconData, OverlayData, IsCustom) = await associatedInstance.FilesystemViewModel.LoadIconOverlayAsync(Path, 24u);

                        await CoreApplication.MainView.ExecuteOnUIThreadAsync(async () =>
                        {
                            Icon = await IconData.ToBitmapAsync();
                        });

                        return;
                    }

                    StorageFile file = await StorageItemHelpers.ToStorageItem<StorageFile>(Path, associatedInstance);

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

        #endregion Private Helpers

        #region Public Helpers

        public async void OpenItem()
        {
            await associatedInstance.InteractionOperations.OpenPath(Path, TargetType);
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

        #endregion
        
        #region IDisposable

        public void Dispose()
        {
            Path = null;
            Icon = null;

            OpenInNewTabCommand = null;
            OpenItemLocationCommand = null;
            RemoveItemCommand = null;

            associatedInstance = null;
        }

        #endregion IDisposable
    }
}