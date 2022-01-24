using Files.Helpers;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Preferences : Page
    {
        public Preferences()
        {
            InitializeComponent();
            _ = DetectOpenFilesAtStartup();
        }

        private async void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(@"ms-settings:startupapps"));
        }

        public async Task DetectOpenFilesAtStartup()
        {
            var stateMode = await ReadState();
            

            switch (stateMode)
            {
                case StartupTaskState.DisabledByPolicy:

                    StateLogin.Message = "Esta funcionalidade está desativada devido às politicas de grupo.";
                    StateLogin.ActionButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;
                case StartupTaskState.DisabledByUser:
                    StateLogin.Message = "Esta funcionalidade está desativada devido às definições do utilizador. Para ativar, vá às definições.";
                    StateLogin.ActionButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    break;
                case StartupTaskState.EnabledByPolicy:
                    StateLogin.Message = "Esta funcionalidade está ativada devido às politicas de grupo.";
                    StateLogin.ActionButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    break;
                default:
                    //StateLogin.Message = "Esta funcionalidade está disponível para ser utilizada.";
                    //StateLogin.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
                    //StateLogin.ActionButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    //StateLogin.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    OpenInLoginControl.Children.Remove(UIElement)this.FindName("StateLogin");
                    break;
            }
        }

        public async Task<StartupTaskState> ReadState()
        {
            var state = await StartupTask.GetAsync("3AA55462-A5FA-4933-88C4-712D0B6CDEBB");
            return state.State;
        }

    }
}