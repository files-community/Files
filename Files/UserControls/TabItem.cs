using GalaSoft.MvvmLight;
using Microsoft.UI.Xaml.Controls;

namespace Files.UserControls
{
    public class TabItem : ViewModelBase
    {
        private string _Header;
        public string Header { get => _Header; set => Set(ref _Header, value); }

        private string _Description = null;
        public string Description { get => _Description; set => Set(ref _Description, value); }

        private IconSource _IconSource;
        public IconSource IconSource { get => _IconSource; set => Set(ref _IconSource, value); }

        public object Content { get; set; }

    }
}