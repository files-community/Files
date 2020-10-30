using Files.View_Models;
using Windows.UI.Xaml.Controls;


namespace Files.UserControls
{
    public sealed partial class StatusBarControl : UserControl
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; private set; } = null;
        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; private set; } = null;

        public StatusBarControl()
        {
            this.InitializeComponent();
        }
    }
}