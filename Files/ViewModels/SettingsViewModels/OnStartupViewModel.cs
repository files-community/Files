using Files.Common;
using Files.Filesystem;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Files.ViewModels.SettingsViewModels
{
    public class OnStartupViewModel : ObservableObject
    {
        private bool openNewTabPageOnStartup = App.AppSettings.OpenNewTabPageOnStartup;
        private bool continueLastSessionOnStartUp = App.AppSettings.ContinueLastSessionOnStartUp;
        private bool openASpecificPageOnStartup = App.AppSettings.OpenASpecificPageOnStartup;
        private int selectedPageIndex = -1;
        private bool isPageListEditEnabled;
        private bool alwaysOpenANewInstance = App.AppSettings.AlwaysOpenANewInstance;

        public OnStartupViewModel()
        {
            if (App.AppSettings.PagesOnStartupList != null)
            {
                PagesOnStartupList = new ObservableCollection<PageOnStartupViewModel>(App.AppSettings.PagesOnStartupList.Select((p) => new PageOnStartupViewModel(p)));
            }
            else
            {
                PagesOnStartupList = new ObservableCollection<PageOnStartupViewModel>();
            }

            PagesOnStartupList.CollectionChanged += PagesOnStartupList_CollectionChanged;
        }

        private void PagesOnStartupList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (PagesOnStartupList.Count() > 0)
            {
                App.AppSettings.PagesOnStartupList = PagesOnStartupList.Select((p) => p.Path).ToArray();
            }
            else
            {
                App.AppSettings.PagesOnStartupList = null;
            }
        }

        public bool OpenNewTabPageOnStartup
        {
            get
            {
                return openNewTabPageOnStartup;
            }
            set
            {
                if (SetProperty(ref openNewTabPageOnStartup, value))
                {
                    App.AppSettings.OpenNewTabPageOnStartup = value;
                }
            }
        }

        public bool ContinueLastSessionOnStartUp
        {
            get
            {
                return continueLastSessionOnStartUp;
            }
            set
            {
                if (SetProperty(ref continueLastSessionOnStartUp, value))
                {
                    App.AppSettings.ContinueLastSessionOnStartUp = value;
                }
            }
        }

        public bool OpenASpecificPageOnStartup
        {
            get
            {
                return openASpecificPageOnStartup;
            }
            set
            {
                if (SetProperty(ref openASpecificPageOnStartup, value))
                {
                    App.AppSettings.OpenASpecificPageOnStartup = value;
                }
            }
        }

        public ObservableCollection<PageOnStartupViewModel> PagesOnStartupList { get; set; }

        public int SelectedPageIndex
        {
            get { return selectedPageIndex; }
            set
            {
                if (SetProperty(ref selectedPageIndex, value))
                {
                    IsPageListEditEnabled = value >= 0;
                }
            }
        }

        public bool IsPageListEditEnabled
        {
            get => isPageListEditEnabled;
            set => SetProperty(ref isPageListEditEnabled, value);
        }

        public ReadOnlyCollection<SidebarItemViewModel> MainPageSidebarItems
        {
            get => MainPage.SideBarItems
                .Where((i) => !(i is HeaderTextItem))
                .Select((i) => new SidebarItemViewModel(i, AddPageCommand))
                .ToList()
                .AsReadOnly();
        }

        public RelayCommand ChangePageCommand => new RelayCommand(ChangePage);
        public RelayCommand RemovePageCommand => new RelayCommand(RemovePage);
        public RelayCommand<string> AddPageCommand => new RelayCommand<string>(AddPage);

        public bool AlwaysOpenANewInstance
        {
            get
            {
                return alwaysOpenANewInstance;
            }
            set
            {
                if (SetProperty(ref alwaysOpenANewInstance, value))
                {
                    App.AppSettings.AlwaysOpenANewInstance = alwaysOpenANewInstance;
                }
            }
        }

        private async void ChangePage()
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                if (SelectedPageIndex >= 0)
                {
                    PagesOnStartupList[SelectedPageIndex] = new PageOnStartupViewModel(folder.Path);
                }
            }
        }

        private void RemovePage()
        {
            int index = SelectedPageIndex;
            if (index >= 0)
            {
                PagesOnStartupList.RemoveAt(index);
                if (index > 0)
                {
                    SelectedPageIndex = index - 1;
                }
                else if (PagesOnStartupList.Count > 0)
                {
                    SelectedPageIndex = 0;
                }
            }
        }

        private async void AddPage(string path = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                var folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    path = folder.Path;
                }
            }

            if (path != null && PagesOnStartupList != null)
            {
                PagesOnStartupList.Add(new PageOnStartupViewModel(path));
            }
        }

        public class PageOnStartupViewModel
        {
            public string Text
            {
                get
                {
                    if (Path == "Home")
                    {
                        return "SidebarHome".GetLocalized();
                    }
                    if (Path == App.AppSettings.RecycleBinPath)
                    {
                        return ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
                    }
                    return Path;
                }
            }

            public string Path { get; }

            internal PageOnStartupViewModel(string path) => Path = path;
        }

        public class SidebarItemViewModel
        {
            public string Text { get; }

            public string Path { get; }

            public RelayCommand<string> OnSelect { get; }

            internal SidebarItemViewModel(INavigationControlItem source, RelayCommand<string> onSelect)
            {
                Text = source.Text;
                Path = source.Path;
                OnSelect = onSelect;
            }
        }
    }
}