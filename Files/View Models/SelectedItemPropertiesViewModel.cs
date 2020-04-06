using Files.Filesystem;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media;

namespace Files.View_Models
{
    public class SelectedItemPropertiesViewModel : ViewModelBase
    {
        public string ItemName 
        { 
            get 
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItems[0].FileName;
                }
                else
                {
                    return null;
                }
            } 
        }
        public string ItemType
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItems[0].FileType;
                }
                else
                {
                    return null;
                }
            }
        }
        public string ItemPath
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItems[0].FilePath;
                }
                else
                {
                    return null;
                }
            }
        }
        public string ItemSize 
        { 
            get 
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItems[0].FileSize;
                }
                else
                {
                    return null;
                }
            } 
        }
        public string ItemCreatedTimestamp
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    DateTimeOffset dateCreated;
                    if (GetStorageItemTypeFromPathAsync(App.CurrentInstance.ContentPage.SelectedItem.FilePath).Result == typeof(StorageFolder))
                    {
                        dateCreated = StorageFolder.GetFolderFromPathAsync(App.CurrentInstance.ContentPage.SelectedItem.FilePath).GetResults().DateCreated;
                    }
                    else if (GetStorageItemTypeFromPathAsync(App.CurrentInstance.ContentPage.SelectedItem.FilePath).Result == typeof(StorageFile))
                    {
                        dateCreated = StorageFile.GetFileFromPathAsync(App.CurrentInstance.ContentPage.SelectedItem.FilePath).GetResults().DateCreated;
                    }
                    return ListedItem.GetFriendlyDate(dateCreated);
                }
                else
                {
                    return null;
                }
            }
        }
        public string ItemModifiedTimestamp
        { 
            get 
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItems[0].FileDate;
                }
                else
                {
                    return null;
                }
            } 
        }
        public ImageSource FileIconSource
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItems[0].FileImg;
                }
                else
                {
                    return null;
                }
            }
        }
        public bool LoadFolderGlyph
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return (App.CurrentInstance.ContentPage.SelectedItems[0].FolderImg == Windows.UI.Xaml.Visibility.Visible) ? true : false;
                }
                else
                {
                    return false;
                }
            }
        }
        public bool LoadUnknownTypeGlyph
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return (App.CurrentInstance.ContentPage.SelectedItems[0].EmptyImgVis == Windows.UI.Xaml.Visibility.Visible) ? true : false;
                }
                else
                {
                    return false;
                }
            }
        }
        public bool LoadFileIcon
        {
            get
            {
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return (App.CurrentInstance.ContentPage.SelectedItems[0].FileIconVis == Windows.UI.Xaml.Visibility.Visible) ? true : false;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<Type> GetStorageItemTypeFromPathAsync(string path)
        {
            IStorageItem selectedStorageItem;
            try
            {
                selectedStorageItem = await StorageFolder.GetFolderFromPathAsync(path);
                return typeof(StorageFolder);
            }
            catch (Exception)
            {
                // Not a folder, so attempt to check for StorageFile
                selectedStorageItem = await StorageFile.GetFileFromPathAsync(path);
                return typeof(StorageFile);
            }
        }
    }
}
