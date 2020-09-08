using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.View_Models
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