using Files.View_Models;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class StatusBarControl : UserControl
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel => selectedViewModel;
        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel => directoryViewModel;
        
        private SelectedItemsPropertiesViewModel selectedViewModel = null;
        private DirectoryPropertiesViewModel directoryViewModel = null;
        public StatusBarControl()
        {
            this.InitializeComponent();
            selectedViewModel = new SelectedItemsPropertiesViewModel();
            directoryViewModel = new DirectoryPropertiesViewModel();
        }
    }
}