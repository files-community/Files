using Files.Interacts;
using Files.View_Models;
using System;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;


namespace Files.UserControls
{
    public sealed partial class StatusBarControl : UserControl
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; set; } = null;
        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; set; } = null;
        public ICommand SelectAllInvokedCommand { get; set; }
        public ICommand InvertSelectionInvokedCommand { get; set; }
        public ICommand ClearSelectionInvokedCommand { get; set; }

        public StatusBarControl()
        {
            this.InitializeComponent();
        }
    }
}