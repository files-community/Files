using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Files.UserControls
{
    public class TabItem : ObservableObject
    {
        private string _Header;

        public string Header
        {
            get => _Header;
            set => SetProperty(ref _Header, value);
        }

        private string _Path;

        // Please, remember to update the Path whenever navigating to directory,
        // currently the update is done in both methods SetSelectedTabInfo in
        // classes HorizontalMultitaskingControl and VerticalTabView
        public string Path
        {
            get => _Path;
            set => SetProperty(ref _Path, value);
        }

        private string _Description = null;

        public string Description
        {
            get => _Description;
            set => SetProperty(ref _Description, value);
        }

        private IconSource _IconSource;

        public IconSource IconSource
        {
            get => _IconSource;
            set => SetProperty(ref _IconSource, value);
        }

        public object Content { get; set; }

        private bool _AllowStorageItemDrop = false;

        public bool AllowStorageItemDrop
        {
            get => _AllowStorageItemDrop;
            set => SetProperty(ref _AllowStorageItemDrop, value);
        }
    }

    public class TabItemContent
    {
        public Type InitialPageType { get; set; }
        public string NavigationArg { get; set; }
    }
}