using GalaSoft.MvvmLight;
using System;

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