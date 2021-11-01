using Files.Filesystem.Search;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.ComponentModel;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface ILocationPickerViewModel : IPickerViewModel
    {
        public bool SearchInSubFolders { get; set; }
    }

    public class LocationPickerViewModel : ObservableObject, ILocationPickerViewModel
    {
        private readonly ISearchLocation location;

        public bool IsEmpty => SearchInSubFolders;

        public bool SearchInSubFolders
        {
            get => location.SearchInSubFolders;
            set => location.SearchInSubFolders = value;
        }

        public ICommand ClearCommand { get; }

        public LocationPickerViewModel(ISearchLocation location)
        {
            this.location = location;
            ClearCommand = new RelayCommand(Clear);
            location.PropertyChanged += Location_PropertyChanged;
        }

        private void Clear() => SearchInSubFolders = true;

        private void Location_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }
    }
}
