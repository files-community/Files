using Files.ViewModels;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
    public sealed partial class PropertiesDialog : ContentDialog
    {
        public PropertiesDialog()
        {
            this.InitializeComponent();
        }

        public SettingsViewModel AppSettings => App.AppSettings;
    }
}