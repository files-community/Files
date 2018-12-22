using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;



namespace Files
{
    
    public sealed partial class AddItem : Page
    {
        public AddItem()
        {
            this.InitializeComponent();
            AddItemsToList();
        }

        public static List<AddListItem> AddItemsList = new List<AddListItem>();
        
        public static void AddItemsToList()
        {
            AddItemsList.Clear();
            AddItemsList.Add(new AddListItem { Header = "Folder", SubHeader = "Creates an empty folder.", isEnabled = true });
            AddItemsList.Add(new AddListItem { Header = "Text Document", SubHeader = "Creates a simple file for text input", isEnabled = true });
            AddItemsList.Add(new AddListItem { Header = "Bitmap Image", SubHeader = "Creates an empty bitmap image file.", isEnabled = true });
            AddItemsList.Add(new AddListItem { Header = "Shortcut", SubHeader = "In Development: Creates a shortcut to a different location.", isEnabled = false });
            AddItemsList.Add(new AddListItem { Header = "Compressed Folder", SubHeader = "In Development: Creates a new compressed folder from an existing folder.", isEnabled = false });
        }

        
        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }

    public class AddListItem
    {
        public string Header { get; set; }
        public string SubHeader { get; set; }

        public bool isEnabled { get; set; }
    }
}
