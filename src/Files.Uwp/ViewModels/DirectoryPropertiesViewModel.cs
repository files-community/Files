using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.Uwp.ViewModels
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