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
                if (areHiddenItemsVisible != value)
                {
                    areHiddenItemsVisible = value;
                    App.AppSettings.AreHiddenItemsVisible = areHiddenItemsVisible;
                    OnPropertyChanged(nameof(AreHiddenItemsVisible));
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
                if (areSystemItemsHidden != value)
                {
                    areSystemItemsHidden = value;
                    App.AppSettings.AreSystemItemsHidden = areSystemItemsHidden;
                    OnPropertyChanged(nameof(AreSystemItemsHidden));
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
                if (showFileExtensions != value)
                {
                    showFileExtensions = value;
                    App.AppSettings.ShowFileExtensions = showFileExtensions;
                    OnPropertyChanged(nameof(ShowFileExtensions));
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
                if (openItemsWithOneclick != value)
                {
                    openItemsWithOneclick = value;
                    App.AppSettings.OpenItemsWithOneclick = openItemsWithOneclick;
                    OnPropertyChanged(nameof(OpenItemsWithOneclick));
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
                if (listAndSortDirectoriesAlongsideFiles != value)
                {
                    listAndSortDirectoriesAlongsideFiles = value;
                    App.AppSettings.ListAndSortDirectoriesAlongsideFiles = listAndSortDirectoriesAlongsideFiles;
                    OnPropertyChanged(nameof(ListAndSortDirectoriesAlongsideFiles));
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
                if (searchUnindexedItems != value)
                {
                    searchUnindexedItems = value;
                    App.AppSettings.SearchUnindexedItems = searchUnindexedItems;
                    OnPropertyChanged(nameof(SearchUnindexedItems));
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
                if (areLayoutPreferencesPerFolder != value)
                {
                    areLayoutPreferencesPerFolder = value;
                    App.AppSettings.AreLayoutPreferencesPerFolder = areLayoutPreferencesPerFolder;
                    OnPropertyChanged(nameof(AreLayoutPreferencesPerFolder));
                }
            }
        }
    }
}
