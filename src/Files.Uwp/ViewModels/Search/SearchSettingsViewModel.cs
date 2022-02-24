using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.Filesystem.Search;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface ISearchSettingsViewModel : ISearchFilterViewModel
    {
        bool SearchInSubFolders { get; set; }

        ISearchFilterViewModelCollection Collection { get; }
    }

    public interface ISearchSettingsPageViewModel : ISearchPageViewModel
    {
        new ISearchSettingsViewModel Filter { get; }
    }

    internal class SearchSettingsViewModel : ObservableObject, ISearchSettingsViewModel
    {
        private readonly ISearchSettings settings;

        public ISearchFilter Filter => settings.Filter;

        public ISearchHeaderViewModel Header => Collection.Header;

        public bool SearchInSubFolders
        {
            get => settings.SearchInSubFolders;
            set => settings.SearchInSubFolders = value;
        }

        public ISearchFilterViewModelCollection Collection { get; }

        public IEnumerable<ISearchTagViewModel> Tags => Collection.Tags;

        public bool IsEmpty => settings.IsEmpty;

        private readonly RelayCommand clearCommand;
        public ICommand ClearCommand => clearCommand;

        public SearchSettingsViewModel(ISearchSettings settings)
        {
            this.settings = settings;
            Collection = new SettingsFilterViewModelCollection(settings.Filter);
            clearCommand = new RelayCommand(settings.Clear, () => !settings.IsEmpty);

            settings.PropertyChanged += Settings_PropertyChanged;
            Collection.PropertyChanged += Filter_PropertyChanged;
        }

        public void Clear() => settings.Clear();

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ISearchFilter.IsEmpty):
                    OnPropertyChanged(nameof(IsEmpty));
                    clearCommand.NotifyCanExecuteChanged();
                    break;
                case nameof(ISearchSettings.SearchInSubFolders):
                    OnPropertyChanged(nameof(SearchInSubFolders));
                    break;
            }
        }

        private void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ISearchFilter.Header):
                    OnPropertyChanged(nameof(Header));
                    break;
                case nameof(ISearchFilter.Tags):
                    OnPropertyChanged(nameof(Tags));
                    break;
            }
        }
    }

    internal class SearchSettingsPageViewModel : ObservableObject, ISearchSettingsPageViewModel
    {
        public ISearchPageViewModel Parent => null;

        ISearchFilterViewModel ISearchPageViewModel.Filter => Filter;
        public ISearchSettingsViewModel Filter { get; }

        public SearchSettingsPageViewModel(ISearchSettingsViewModel settings) => Filter = settings;
    }

    internal class SettingsFilterViewModelCollection : SearchFilterViewModelCollection
    {
        public override string Description => null;

        public SettingsFilterViewModelCollection(ISearchFilterCollection filter) : base(filter) {}
    }
}
