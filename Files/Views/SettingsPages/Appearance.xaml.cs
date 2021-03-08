using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Appearance : Page
    {
        public Appearance()
        {
            InitializeComponent();
        }

        public bool ShowColorSchemeSelector => App.ExternalResourcesHelper.Themes.Count > 1;
    }
}