using Files.Filesystem;
using Files.Interacts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public sealed partial class Properties : Page
    {
        ListedItem Item { get; set; }
        ContentDialog PropertiesDialog { get; set; }
        public Properties()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var result = e.Parameter as IEnumerable;
            IList<ListedItem> listedItems = result.OfType<ListedItem>().ToList();
            Item = listedItems[0];
           PropertiesDialog = Frame.Tag as ContentDialog;
            base.OnNavigatedTo(e);
        }
        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Apply_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PropertiesDialog?.Hide();
        }
    }
}
