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
using Windows.Storage;
using Windows.Storage.Pickers;
using Files.View_Models;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class SpecificPageOnStartup : UserControl
    {
        //public SettingsViewModel AppSettings => App.AppSettings;
        public delegate void RemovePageEventHandler(SpecificPageOnStartup pageItem, string path);
        public delegate void EditPageEventHandler(string old_path, string new_path);
        public event RemovePageEventHandler removePageEvent;
        public event EditPageEventHandler editPageEvent;
        public SpecificPageOnStartup(string path = "New Tab")
        {
            InitializeComponent();
            PagePath.Text = path;
        }

        public void DeletePage_Click(object sender, RoutedEventArgs e)
        {
            removePageEvent(this, PagePath.Text);
        }

        public async void EditPage_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                editPageEvent(PagePath.Text, folder.Path);
                PagePath.Text = folder.Path;
            }
        }
    }
}
