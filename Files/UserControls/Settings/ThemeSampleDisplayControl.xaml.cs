using Files.Helpers;
using Files.ViewModels.SettingsViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

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
                ReevaluateThemeResourceBinding();
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
            ReevaluateThemeResourceBinding();
        }

        public async Task<bool> ReevaluateThemeResourceBinding()
        {
            //var fallBackDictionaryWinUI = Resources.MergedDictionaries[0].ThemeDictionaries[ThemeHelper.RootTheme.ToString()] as ResourceDictionary;
            // var fallBackDictionaryFiles = Resources.MergedDictionaries[1].ThemeDictionaries[ThemeHelper.RootTheme.ToString()] as ResourceDictionary;
            if (RootGrid != null)
            {
                if(SampleTheme.Path != null)
                {
                    var resources = await App.ExternalResourcesHelper.TryLoadResourceDictionary(SampleTheme);
                    if(resources != null)
                    {
                        Resources.MergedDictionaries.Add(resources);
                        RequestedTheme = ElementTheme.Dark;
                        RequestedTheme = ElementTheme.Light;
                        RequestedTheme = ThemeHelper.RootTheme;
                    }
                }
                //try
                //{
                //    dictionary = TryGetCorrectThemeResourceDictionary(resources, ThemeHelper.RootTheme);
                //}
                //var dictionary = SampleTheme.LoadedResources;
                //if (SampleTheme.LoadedResources == null)
                //{
                //    if (SampleTheme.Path != null)
                //    {
                //        catch (Exception) { }
                //    }
                //}
                //else
                //{
                //    try
                //    {
                //        Resources.MergedDictionaries.Add(SampleTheme.LoadedResources);
                //        dictionary = TryGetCorrectThemeResourceDictionary(SampleTheme.LoadedResources, ThemeHelper.RootTheme);
                //    }
                //    catch (Exception) { }
                //}

                //try
                //{
                //    LeftSidePanel.Background = TryGetResource("SolidBackgroundFillColorSecondaryBrush", dictionary, null);
                //    TopNavigationPanel.Background = TryGetResource("SolidBackgroundFillColorSecondaryBrush", dictionary, null);
                //    SelectedTabMockUp.Fill = TryGetResource("TabViewItemHeaderBackgroundSelected", dictionary, null);
                //}
                //catch (Exception) { }
            }
            return true;
        }
    }
}
