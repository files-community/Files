using Files.UserControls;
using Files.View_Models;
using System;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class OnStartup : Page
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public OnStartup()
        {
            InitializeComponent();

            if (AppSettings.OpenASpecificPageOnStartup && AppSettings.PagesOnStartupList?.Length > 0)
            {
                foreach (string path in AppSettings.PagesOnStartupList)
                {
                    CreateAndAddPageItem(path);
                }
            }
        }

        private async void btnAddPage_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                if (AppSettings.PagesOnStartupList != null)
                {
                    AppSettings.PagesOnStartupList = AppSettings.PagesOnStartupList.Append(folder.Path).ToArray();
                }
                else
                {
                    AppSettings.PagesOnStartupList = new string[] { folder.Path };
                }
                CreateAndAddPageItem(folder.Path);
            }
        }

        public SpecificPageOnStartup CreateAndAddPageItem(string path)
        {
            SpecificPageOnStartup newPageItem = new SpecificPageOnStartup(path);
            newPageItem.changePageEvent += EditPage;
            newPageItem.removePageEvent += RemovePage;
            newPageItem.HorizontalAlignment = HorizontalAlignment.Left;
            PagesPanel.Children.Add(newPageItem);
            return newPageItem;
        }

        public void EditPage(string old_path, string new_path)
        {
            for (int i = 0; i < AppSettings.PagesOnStartupList.Length; i++)
            {
                if (AppSettings.PagesOnStartupList[i] == old_path)
                {
                    AppSettings.PagesOnStartupList[i] = new_path;
                    break;
                }
            }
        }

        public void RemovePage(SpecificPageOnStartup pageItem, string path)
        {
            PagesPanel.Children.Remove(pageItem);
            string[] newPages = AppSettings.PagesOnStartupList.Where(s => s != path).ToArray();
            if (newPages.Length > 0)
            {
                AppSettings.PagesOnStartupList = newPages;
            }
            else
            {
                AppSettings.PagesOnStartupList = null;
            }
        }
    }
}