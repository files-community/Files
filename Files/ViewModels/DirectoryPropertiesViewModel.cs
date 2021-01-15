using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels
{
    public class DirectoryPropertiesViewModel : ObservableObject
    {
        private string directoryItemCount;

        public string DirectoryItemCount
        {
            get => directoryItemCount;
            set => SetProperty(ref directoryItemCount, value);
        }
    }
}