using GalaSoft.MvvmLight;

namespace Files.View_Models
{
    public class DirectoryPropertiesViewModel : ViewModelBase
    {
        private string _DirectoryItemCount;

        public string DirectoryItemCount
        {
            get => _DirectoryItemCount;
            set => Set(ref _DirectoryItemCount, value);
        }
    }
}