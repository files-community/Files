using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
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
                PagesOnStartupList = new ObservableCollection<string>(App.AppSettings.PagesOnStartupList);
            }
            else
            {
                PagesOnStartupList = new ObservableCollection<string>();
            }

            PagesOnStartupList.CollectionChanged += PagesOnStartupList_CollectionChanged;
        }

        private void PagesOnStartupList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (PagesOnStartupList.Count() > 0)
            {
                App.AppSettings.PagesOnStartupList = PagesOnStartupList.ToArray();
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

        public ObservableCollection<string> PagesOnStartupList { get; set; }

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

        public RelayCommand ChangePageCommand => new RelayCommand(() => ChangePage());
        public RelayCommand RemovePageCommand => new RelayCommand(() => RemovePage());
        public RelayCommand AddPageCommand => new RelayCommand(() => AddPage());

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
                    PagesOnStartupList[SelectedPageIndex] = folder.Path;
                }
            }
        }

        private void RemovePage()
        {
            if (SelectedPageIndex >= 0)
            {
                PagesOnStartupList.RemoveAt(SelectedPageIndex);
            }
        }

        private async void AddPage()
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                if (PagesOnStartupList != null)
                {
                    PagesOnStartupList.Add(folder.Path);
                }
            }
        }
    }
}