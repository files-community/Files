using Files.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Windows.Storage.AccessCache;

namespace Files.ViewModels.SettingsViewModels
{
    public class FilesAndFoldersViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public bool AreHiddenItemsVisible
        {
            get => UserSettingsService.FilesAndFoldersSettingsService.AreHiddenItemsVisible;
            set
            {
                if (value != UserSettingsService.FilesAndFoldersSettingsService.AreHiddenItemsVisible)
                {
                    UserSettingsService.FilesAndFoldersSettingsService.AreHiddenItemsVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AreSystemItemsHidden
        {
            get => UserSettingsService.FilesAndFoldersSettingsService.AreSystemItemsHidden;
            set
            {
                if (value != UserSettingsService.FilesAndFoldersSettingsService.AreSystemItemsHidden)
                {
                    UserSettingsService.FilesAndFoldersSettingsService.AreSystemItemsHidden = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowFileExtensions
        {
            get => UserSettingsService.FilesAndFoldersSettingsService.ShowFileExtensions;
            set
            {
                if (value != UserSettingsService.FilesAndFoldersSettingsService.ShowFileExtensions)
                {
                    UserSettingsService.FilesAndFoldersSettingsService.ShowFileExtensions = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool OpenFilesWithOneClick
        {
            get => UserSettingsService.FilesAndFoldersSettingsService.OpenFilesWithOneClick;
            set
            {
                if (value != UserSettingsService.FilesAndFoldersSettingsService.OpenFilesWithOneClick)
                {
                    UserSettingsService.FilesAndFoldersSettingsService.OpenFilesWithOneClick = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool OpenFoldersWithOneClick
        {
            get => UserSettingsService.FilesAndFoldersSettingsService.OpenFoldersWithOneClick;
            set
            {
                if (value != UserSettingsService.FilesAndFoldersSettingsService.OpenFoldersWithOneClick)
                {
                    UserSettingsService.FilesAndFoldersSettingsService.OpenFoldersWithOneClick = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ListAndSortDirectoriesAlongsideFiles
        {
            get => UserSettingsService.FilesAndFoldersSettingsService.ListAndSortDirectoriesAlongsideFiles;
            set
            {
                if (value != UserSettingsService.FilesAndFoldersSettingsService.ListAndSortDirectoriesAlongsideFiles)
                {
                    UserSettingsService.FilesAndFoldersSettingsService.ListAndSortDirectoriesAlongsideFiles = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool SearchUnindexedItems
        {
            get => UserSettingsService.FilesAndFoldersSettingsService.SearchUnindexedItems;
            set
            {
                if (value != UserSettingsService.FilesAndFoldersSettingsService.SearchUnindexedItems)
                {
                    UserSettingsService.FilesAndFoldersSettingsService.SearchUnindexedItems = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AreLayoutPreferencesPerFolder
        {
            get => UserSettingsService.FilesAndFoldersSettingsService.AreLayoutPreferencesPerFolder;
            set
            {
                if (value != UserSettingsService.FilesAndFoldersSettingsService.AreLayoutPreferencesPerFolder)
                {
                    UserSettingsService.FilesAndFoldersSettingsService.AreLayoutPreferencesPerFolder = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSavingRecentItemsEnabled
        {
            get => UserSettingsService.FilesAndFoldersSettingsService.IsSavingRecentItemsEnabled;
            set
            {
                if (value != UserSettingsService.FilesAndFoldersSettingsService.IsSavingRecentItemsEnabled)
                {
                    UserSettingsService.FilesAndFoldersSettingsService.IsSavingRecentItemsEnabled = value;
                    
                    if (!UserSettingsService.FilesAndFoldersSettingsService.IsSavingRecentItemsEnabled)
                    {
                        var mru = StorageApplicationPermissions.MostRecentlyUsedList;
                        mru.Clear();
                    }
                    
                    OnPropertyChanged();
                }
            }
        }
    }
}