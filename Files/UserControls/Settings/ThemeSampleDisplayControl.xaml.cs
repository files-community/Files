using Files.Helpers;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls.Settings
{
    public sealed partial class ThemeSampleDisplayControl : UserControl
    {
        public AppTheme SampleTheme
        {
            get { return (AppTheme)GetValue(SampleThemeProperty); }
            set
            {
                SetValue(SampleThemeProperty, value);
                _ = ReevaluateThemeResourceBinding();
            }
        }

        // Using a DependencyProperty as the backing store for SampleTheme.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SampleThemeProperty =
            DependencyProperty.Register("SampleTheme", typeof(AppTheme), typeof(ThemeSampleDisplayControl), new PropertyMetadata(null));

        public ThemeSampleDisplayControl()
        {
            this.InitializeComponent();
            Loaded += ThemeSampleDisplayControl_Loaded;
        }

        private void ThemeSampleDisplayControl_Loaded(object sender, RoutedEventArgs e)
        {
            _ = ReevaluateThemeResourceBinding();
        }

        public async Task<bool> ReevaluateThemeResourceBinding()
        {
            try
            {
                var resources = await App.ExternalResourcesHelper.TryLoadResourceDictionary(SampleTheme);
                if (resources != null)
                {
                    Resources.MergedDictionaries.Add(resources);
                    RequestedTheme = ElementTheme.Dark;
                    RequestedTheme = ElementTheme.Light;
                    RequestedTheme = ThemeHelper.RootTheme;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, $"Error loading theme: {SampleTheme?.Path}");
                return false;
            }
        }
    }
}
