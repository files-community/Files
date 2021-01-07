using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels
{
    public class DirectoryPropertiesViewModel : ObservableObject
    {
        private string _DirectoryItemCount;

        public string DirectoryItemCount
        {
            get => _DirectoryItemCount;
            set => SetProperty(ref _DirectoryItemCount, value);
        }
    }
}