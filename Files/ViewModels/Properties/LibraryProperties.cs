using ByteSizeLib;
using Files.Enums;
using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Microsoft.Toolkit.Uwp;
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
        public LibraryItem Library { get; private set; }

        public LibraryProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, CoreDispatcher coreDispatcher, LibraryItem item, IShellPage instance)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            Dispatcher = coreDispatcher;
            Library = item;
            AppInstance = instance;

            GetBaseProperties();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        public void UpdateLibrary(LibraryItem library)
        {
            Library = library;
            GetBaseProperties();
            GetSpecialProperties();
        }

        public override void GetBaseProperties()
        {
            if (Library != null)
            {
                ViewModel.ItemName = Library.ItemName;
                ViewModel.OriginalItemName = Library.ItemName;
                ViewModel.ItemType = Library.ItemType;
                ViewModel.LoadCustomIcon = Library.LoadCustomIcon;
                ViewModel.CustomIconSource = Library.CustomIconSource;
                ViewModel.LoadFileIcon = Library.LoadFileIcon;
                ViewModel.ContainsFilesOrFolders = false;
            }
        }

        public async override void GetSpecialProperties()
        {
            ViewModel.IsReadOnly = NativeFileOperationsHelper.HasFileAttribute(Library.ItemPath, System.IO.FileAttributes.ReadOnly);
            ViewModel.IsHidden = NativeFileOperationsHelper.HasFileAttribute(Library.ItemPath, System.IO.FileAttributes.Hidden);

            var fileIconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Library.ItemPath, 80);
            if (fileIconData != null)
            {
                ViewModel.IconData = fileIconData;
                ViewModel.LoadCustomIcon = false;
                ViewModel.LoadFileIcon = true;
            }

            BaseStorageFile libraryFile = await AppInstance.FilesystemViewModel.GetFileFromPathAsync(Library.ItemPath);
            if (libraryFile != null)
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
                ViewModel.ItemCreatedTimestamp = libraryFile.DateCreated.GetFriendlyDateFromFormat(returnformat);
                if (libraryFile.Properties != null)
                {
                    GetOtherProperties(libraryFile.Properties);
                }
            }

            var storageFolders = new List<BaseStorageFolder>();
            if (Library.Folders != null)
            {
                try
                {
                    foreach (var path in Library.Folders)
                    {
                        BaseStorageFolder folder = await AppInstance.FilesystemViewModel.GetFolderFromPathAsync(path);
                        if (!string.IsNullOrEmpty(folder.Path))
                        {
                            storageFolders.Add(folder);
                        }
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.Warn(ex, ex.Message);
                }
            }

            if (storageFolders.Count > 0)
            {
                ViewModel.ContainsFilesOrFolders = true;
                ViewModel.LocationsCount = storageFolders.Count;
                GetLibrarySize(storageFolders, TokenSource.Token);
            }
            else
            {
                ViewModel.FilesAndFoldersCountString = "LibraryNoLocations/Text".GetLocalized();
            }
        }

        private async void GetLibrarySize(List<BaseStorageFolder> storageFolders, CancellationToken token)
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
                App.Logger.Warn(ex, ex.Message);
            }

            ViewModel.ItemSizeProgressVisibility = Visibility.Collapsed;

            SetItemsCountString();
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsReadOnly":
                    if (ViewModel.IsReadOnly)
                    {
                        NativeFileOperationsHelper.SetFileAttribute(Library.ItemPath, System.IO.FileAttributes.ReadOnly);
                    }
                    else
                    {
                        NativeFileOperationsHelper.UnsetFileAttribute(Library.ItemPath, System.IO.FileAttributes.ReadOnly);
                    }
                    break;

                case "IsHidden":
                    if (ViewModel.IsHidden)
                    {
                        NativeFileOperationsHelper.SetFileAttribute(Library.ItemPath, System.IO.FileAttributes.Hidden);
                    }
                    else
                    {
                        NativeFileOperationsHelper.UnsetFileAttribute(Library.ItemPath, System.IO.FileAttributes.Hidden);
                    }
                    break;
            }
        }
    }
}