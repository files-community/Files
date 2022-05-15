using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared.Services.DateTimeFormatter;
using Files.Uwp.Extensions;
using Files.Uwp.Filesystem;
using Files.Uwp.Filesystem.StorageItems;
using Files.Uwp.Helpers;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Files.Uwp.ViewModels.Properties
{
    internal class LibraryProperties : BaseProperties
    {
        private static readonly IDateTimeFormatter dateTimeFormatter = Ioc.Default.GetService<IDateTimeFormatter>();

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
                ViewModel.ItemCreatedTimestamp = dateTimeFormatter.ToShortLabel(libraryFile.DateCreated);
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
            ViewModel.ItemSizeVisibility = true;
            ViewModel.ItemSizeProgressVisibility = true;

            try
            {
                long librarySize = 0;
                foreach (var folder in storageFolders)
                {
                    librarySize += await Task.Run(async () => await CalculateFolderSizeAsync(folder.Path, token));
                }
                ViewModel.ItemSizeBytes = librarySize;
                ViewModel.ItemSize = librarySize.ToLongSizeString();
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, ex.Message);
            }

            ViewModel.ItemSizeProgressVisibility = false;

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