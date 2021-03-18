using ByteSizeLib;
using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Files.ViewModels.Properties
{
    internal class LibraryProperties : BaseProperties
    {
        public LibraryItem Item { get; }

        public LibraryProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, CoreDispatcher coreDispatcher, LibraryItem item, IShellPage instance)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            Dispatcher = coreDispatcher;
            Item = item;
            AppInstance = instance;

            GetBaseProperties();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        public override void GetBaseProperties()
        {
            if (Item != null)
            {
                ViewModel.ItemName = Item.ItemName;
                ViewModel.OriginalItemName = Item.ItemName;
                ViewModel.ItemType = Item.ItemType;
                ViewModel.ItemModifiedTimestamp = Item.ItemDateModified;
                ViewModel.ItemCreatedTimestamp = Item.ItemDateCreated;
                //ViewModel.FileIconSource = Item.FileImage;
                ViewModel.LoadCustomGlyph = Item.LoadCustomGlyph;
                ViewModel.CustomGlyph = Item.CustomGlyph;
                ViewModel.LoadFolderGlyph = Item.LoadFolderGlyph;
                ViewModel.LoadUnknownTypeGlyph = Item.LoadUnknownTypeGlyph;
                ViewModel.LoadFileIcon = Item.LoadFileIcon;
                ViewModel.ContainsFilesOrFolders = false;
            }
        }

        public async override void GetSpecialProperties()
        {
            ViewModel.IsHidden = NativeFileOperationsHelper.HasFileAttribute(Item.ItemPath, System.IO.FileAttributes.Hidden);

            var fileIconInfo = await AppInstance.FilesystemViewModel.LoadIconOverlayAsync(Item.ItemPath, 80);
            if (fileIconInfo.IconData != null && fileIconInfo.IsCustom)
            {
                ViewModel.FileIconSource = await fileIconInfo.IconData.ToBitmapAsync();
            }

            var storageFolders = new List<StorageFolder>();
            if (Item.Paths != null)
            {
                try
                {
                    foreach (var path in Item.Paths)
                    {
                        StorageFolder folder = await AppInstance.FilesystemViewModel.GetFolderFromPathAsync(path);
                        if (!string.IsNullOrEmpty(folder.Path))
                        {
                            storageFolders.Add(folder);
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
                }
            }

            if (storageFolders.Count > 0)
            {
                ViewModel.ContainsFilesOrFolders = true;
                // TODO: get props from library file 
                // ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                //string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
                //ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDateFromFormat(storageFolder.DateCreated, returnformat);
                // GetOtherProperties(storageFolder.Properties);
                GetLibrarySize(storageFolders, TokenSource.Token);
            }
        }

        private async void GetLibrarySize(List<StorageFolder> storageFolders, CancellationToken token)
        {
            ViewModel.ItemSizeVisibility = Visibility.Visible;
            ViewModel.ItemSizeProgressVisibility = Visibility.Visible;

            try
            {
                long librarySize = 0;
                foreach (var folder in storageFolders)
                {
                    librarySize += await Task.Run(async () => await CalculateFolderSizeAsync(folder.Path, token));
                }
                ViewModel.ItemSizeBytes = librarySize;
                ViewModel.ItemSize = $"{ByteSize.FromBytes(librarySize).ToBinaryString().ConvertSizeAbbreviation()} ({ByteSize.FromBytes(librarySize).Bytes:#,##0} {"ItemSizeBytes".GetLocalized()})";
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
            }

            ViewModel.ItemSizeProgressVisibility = Visibility.Collapsed;

            SetItemsCountString();
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsHidden":
                    if (ViewModel.IsHidden)
                    {
                        NativeFileOperationsHelper.SetFileAttribute(Item.ItemPath, System.IO.FileAttributes.Hidden);
                    }
                    else
                    {
                        NativeFileOperationsHelper.UnsetFileAttribute(Item.ItemPath, System.IO.FileAttributes.Hidden);
                    }
                    break;
            }
        }
    }
}