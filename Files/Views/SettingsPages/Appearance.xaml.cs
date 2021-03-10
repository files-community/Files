using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Appearance : Page
    {
        public Appearance()
        {
            InitializeComponent();
        }

        private bool ShowColorSchemeSelector => App.ExternalResourcesHelper.Themes.Count > 1;
    }
}