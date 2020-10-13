using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class SpecificPageOnStartup : UserControl
    {
        //public SettingsViewModel AppSettings => App.AppSettings;
        public delegate void RemovePageEventHandler(SpecificPageOnStartup pageItem, string path);

        public delegate void ChangePageEventHandler(string old_path, string new_path);

        public event RemovePageEventHandler removePageEvent;

        public event ChangePageEventHandler changePageEvent;

        public SpecificPageOnStartup(string path = "New Tab")
        {
            InitializeComponent();
            PagePath.Text = path;
        }

        public void RemovePage_Click(object sender, RoutedEventArgs e)
        {
            removePageEvent(this, PagePath.Text);
        }

        public async void ChangePage_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                changePageEvent(PagePath.Text, folder.Path);
                PagePath.Text = folder.Path;
            }
        }
    }
}