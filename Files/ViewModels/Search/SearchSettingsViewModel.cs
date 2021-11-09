using Files.Filesystem.Search;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface ISearchSettingsViewModel
    {
        ISearchPageNavigator Navigator { get; }

        IPickerViewModel LocationViewModel { get; }
        IPickerViewModel FilterViewModel { get; }
    }

    public class SearchSettingsViewModel : ObservableObject, ISearchSettingsViewModel
    {
        public ISearchPageNavigator Navigator { get; }

        public IPickerViewModel LocationViewModel { get; }
        public IPickerViewModel FilterViewModel { get; }

        public ICommand SearchCommand { get; }

        public SearchSettingsViewModel(ISearchPageContext context, ISearchSettings settings)
        {
            Navigator = context;
            var filter = settings.Filter as ISearchFilterCollection;

            LocationViewModel = new LocationPickerViewModel(settings.Location);
            FilterViewModel = new GroupPickerViewModel(context, filter);

            SearchCommand = new RelayCommand(context.Search);
        }
    }
}
