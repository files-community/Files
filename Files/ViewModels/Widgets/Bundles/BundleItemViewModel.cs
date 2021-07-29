﻿using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels.Widgets.Bundles
{
    public class BundleItemViewModel : ObservableObject, IDisposable
    {
        #region Actions

        public Action<string, FilesystemItemType, bool, bool, IEnumerable<string>> OpenPath { get; set; }

        public Action<string> OpenPathInNewPane { get; set; }

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

                if (fileName.EndsWith(".lnk") || fileName.EndsWith(".url"))
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

        public bool OpenInNewTabLoad
        {
            get => TargetType == FilesystemItemType.Directory;
        }

        public bool OpenInNewPaneLoad
        {
            get => App.AppSettings.IsDualPaneEnabled && TargetType == FilesystemItemType.Directory;
        }

        #endregion Public Properties

        #region Commands

        public ICommand OpenInNewTabCommand { get; private set; }

        public ICommand OpenInNewPaneCommand { get; private set; }

        public ICommand OpenItemLocationCommand { get; private set; }

        public ICommand RemoveItemCommand { get; private set; }

        #endregion Commands

        #region Constructor

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

        #endregion Constructor

        #region Command Implementation

        private async void OpenInNewTab()
        {
            await MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), Path);
        }

        private void OpenInNewPane()
        {
            OpenPathInNewPane(Path);
        }

        private void OpenItemLocation()
        {
            OpenPath(System.IO.Path.GetDirectoryName(Path), FilesystemItemType.Directory, false, false, System.IO.Path.GetFileName(Path).CreateEnumerable());
        }

        #endregion Command Implementation

        #region Public Helpers

        public async Task UpdateIcon()
        {
            if (TargetType == FilesystemItemType.Directory) // OpenDirectory
            {
                Icon = FolderIcon;
            }
            else // NotADirectory
            {
                try
                {
                    if (Path.EndsWith(".lnk") || Path.EndsWith(".url"))
                    {
                        byte[] iconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Path, 24u);
                        Icon = iconData != null ? await iconData.ToBitmapAsync() : null;

                        return;
                    }

                    StorageFile file = await StorageItemHelpers.ToStorageItem<StorageFile>(Path);

                    if (file == null) // No file found
                    {
                        Icon = new BitmapImage();
                        return;
                    }

                    BitmapImage icon = new BitmapImage();
                    using var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.ListView, 24u, ThumbnailOptions.UseCurrentScale);

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

        public void OpenItem()
        {
            OpenPath(Path, TargetType, false, false, null);
        }

        public void RemoveItem()
        {
            if (App.BundlesSettings.SavedBundles.ContainsKey(ParentBundleName))
            {
                Dictionary<string, List<string>> allBundles = App.BundlesSettings.SavedBundles;
                allBundles[ParentBundleName].Remove(Path);
                App.BundlesSettings.SavedBundles = allBundles;
                NotifyItemRemoved(this);
            }
        }

        #endregion Public Helpers

        #region IDisposable

        public void Dispose()
        {
            Icon = null;

            OpenPath = null;
            OpenPathInNewPane = null;
        }

        #endregion IDisposable
    }
}