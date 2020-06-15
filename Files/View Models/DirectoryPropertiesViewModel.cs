using Files.Filesystem;
using GalaSoft.MvvmLight;
using System;
using Windows.Storage;
using Windows.UI.Xaml.Media;

namespace Files.View_Models
{
    public class DirectoryPropertiesViewModel : ViewModelBase
    {
        private String _DirectoryItemCount;

        public String DirectoryItemCount
        {
            get => _DirectoryItemCount;
            set => Set(ref _DirectoryItemCount, value);
        }
    }
}