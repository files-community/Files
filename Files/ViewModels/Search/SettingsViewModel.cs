using Files.Filesystem.Search;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.Search
{
    /*public interface ISettingsViewModel
    {
        ILocationViewModel Location { get; }
        IFilterViewModel Filter { get; }
    }

    public class SettingsViewModel : ObservableObject, ISettingsViewModel
    {
        public ILocationViewModel Location { get; }
        public IFilterViewModel Filter { get; }

        public SettingsViewModel() : this(SearchSettings.Default)
        {
        }
        public SettingsViewModel(ISettings settings)
        {
            var factory = new FilterViewModelFactory();

            Location = new LocationViewModel(settings.Location);
            Filter = factory.GetViewModel(settings.Filter);
        }
    }*/
}
