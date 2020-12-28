using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.SettingsViewModels
{
    public class FilesAndFoldersViewModel : ObservableObject
    {
        private bool areHiddenItemsVisible = App.AppSettings.AreHiddenItemsVisible;
        private bool areSystemItemsHidden = App.AppSettings.AreSystemItemsHidden;
        private bool showFileExtensions = App.AppSettings.ShowFileExtensions;
        private bool openItemsWithOneclick = App.AppSettings.OpenItemsWithOneclick;
        private bool listAndSortDirectoriesAlongsideFiles = App.AppSettings.ListAndSortDirectoriesAlongsideFiles;
        private bool searchUnindexedItems = App.AppSettings.SearchUnindexedItems;

        public bool AreHiddenItemsVisible
        {
            get
            {
                return areHiddenItemsVisible;
            }
            set
            {
                if (SetProperty(ref areHiddenItemsVisible, value))
                {
                    App.AppSettings.AreHiddenItemsVisible = areHiddenItemsVisible;
                }
            }
        }

        public bool AreSystemItemsHidden
        {
            get
            {
                return areSystemItemsHidden;
            }
            set
            {
                if (SetProperty(ref areSystemItemsHidden, value))
                {
                    App.AppSettings.AreSystemItemsHidden = areSystemItemsHidden;  
                }
            }
        }

        public bool ShowFileExtensions
        {
            get
            {
                return showFileExtensions;
            }
            set
            {
                if (SetProperty(ref showFileExtensions, value))
                {
                    App.AppSettings.ShowFileExtensions = showFileExtensions;
                }
            }
        }

        public bool OpenItemsWithOneclick
        {
            get
            {
                return openItemsWithOneclick;
            }
            set
            {
                if (SetProperty(ref openItemsWithOneclick, value))
                {
                    App.AppSettings.OpenItemsWithOneclick = openItemsWithOneclick;
                }
            }
        }

        public bool ListAndSortDirectoriesAlongsideFiles
        {
            get
            {
                return listAndSortDirectoriesAlongsideFiles;
            }
            set
            {
                if (SetProperty(ref listAndSortDirectoriesAlongsideFiles, value))
                {
                    App.AppSettings.ListAndSortDirectoriesAlongsideFiles = listAndSortDirectoriesAlongsideFiles;
                }
            }
        }

        public bool SearchUnindexedItems
        {
            get
            {
                return searchUnindexedItems;
            }
            set
            {
                if (SetProperty(ref searchUnindexedItems, value))
                {
                    App.AppSettings.SearchUnindexedItems = searchUnindexedItems;
                }
            }
        }

        private bool areLayoutPreferencesPerFolder = App.AppSettings.AreLayoutPreferencesPerFolder;

        public bool AreLayoutPreferencesPerFolder
        {
            get
            {
                return areLayoutPreferencesPerFolder;
            }
            set
            {
                if (SetProperty(ref areLayoutPreferencesPerFolder, value))
                {
                    App.AppSettings.AreLayoutPreferencesPerFolder = areLayoutPreferencesPerFolder;
                }
            }
        }
    }
}
