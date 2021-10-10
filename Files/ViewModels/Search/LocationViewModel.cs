using Files.Filesystem.Search;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Files.ViewModels.Search
{
    public interface ILocationViewModel : INotifyPropertyChanged
    {
        public bool UseSubFolders { get; set; }
        public bool UseSystemFiles { get; set; }
        public bool UseCompressedFiles { get; set; }
    }

    public class LocationViewModel : ObservableObject, ILocationViewModel
    {
        private readonly ILocation location;

        public bool UseSubFolders
        {
            get => location.Options.HasFlag(LocationOptions.SubFolders);
            set => SetOption(value, LocationOptions.SubFolders);
        }
        public bool UseSystemFiles
        {
            get => location.Options.HasFlag(LocationOptions.SystemFiles);
            set => SetOption(value, LocationOptions.SystemFiles);
        }
        public bool UseCompressedFiles
        {
            get => location.Options.HasFlag(LocationOptions.CompressedFiles);
            set => SetOption(value, LocationOptions.CompressedFiles);
        }

        public LocationViewModel(ILocation location)
        {
            this.location = location;
            location.PropertyChanged += Location_PropertyChanged;
        }

        private void SetOption(bool value, LocationOptions option)
        {
            if (value)
            {
                location.Options |= option;
            }
            else
            {
                location.Options &= ~option;
            }
        }

        private void Location_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILocation.Options))
            {
                OnPropertyChanged(nameof(UseSubFolders));
                OnPropertyChanged(nameof(UseSystemFiles));
                OnPropertyChanged(nameof(UseCompressedFiles));
            }
        }
    }
}
