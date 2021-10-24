using Files.Filesystem.Search;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface ISettingsViewModel
    {
        ILocationViewModel LocationViewModel { get; }
        IPickerViewModel PickerViewModel { get; }

        ICommand SearchCommand { get; }
    }

    public class SettingsViewModel : ObservableObject, ISettingsViewModel
    {
        public ILocationViewModel LocationViewModel { get; }
        public IPickerViewModel PickerViewModel { get; }

        public ICommand SearchCommand { get; }

        public SettingsViewModel(ISearchPageContext context, ISettings settings)
        {
            var filter = settings.Filter as IFilterCollection;

            LocationViewModel = new LocationViewModel(settings.Location);
            PickerViewModel = new GroupPickerViewModel(context, filter)
            {
                Description = filter.Description
            };

            SearchCommand = new RelayCommand(context.Search);
        }
    }
}
