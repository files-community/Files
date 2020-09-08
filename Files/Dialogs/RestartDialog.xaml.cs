using Windows.UI.Xaml.Controls;

namespace Files.Dialogs
{
    public sealed partial class RestartDialog : UserControl
    {
        public RestartDialog()
        {
            this.InitializeComponent();
            RestartNotification.Show();
        }
    }
}
