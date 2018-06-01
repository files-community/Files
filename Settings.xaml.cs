using Files.SettingsPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;



namespace Files
{
    
    public sealed partial class Settings : Page
    {
        public Settings()
        {
            this.InitializeComponent();
            SettingsContentFrame.Navigate(typeof(Personalization));
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            NavigationViewItem item = args.SelectedItem as NavigationViewItem;
            if(item.Name == "Personalization")
            {
                SettingsContentFrame.Navigate(typeof(Personalization));
            }else if(item.Name == "Features")
            {

            }else if(item.Name == "About")
            {

            }
        }
    }
}
