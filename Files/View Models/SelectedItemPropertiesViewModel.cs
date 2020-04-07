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
                    if (GetStorageItemTypeFromPathAsync(App.CurrentInstance.ContentPage.SelectedItem.FilePath) == typeof(StorageFolder))
                    {
                        dateCreated = StorageFolder.GetFolderFromPathAsync(App.CurrentInstance.ContentPage.SelectedItem.FilePath).GetResults().DateCreated;
                    }
                    else if (GetStorageItemTypeFromPathAsync(App.CurrentInstance.ContentPage.SelectedItem.FilePath) == typeof(StorageFile))
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
                    return App.CurrentInstance.ContentPage.SelectedItems[0].FileImage;
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
                    return App.CurrentInstance.ContentPage.SelectedItems[0].LoadFolderGlyph;
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
                    return App.CurrentInstance.ContentPage.SelectedItems[0].LoadUnknownTypeGlyph;
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
                    return App.CurrentInstance.ContentPage.SelectedItems[0].LoadFileIcon;
                }
                else
                {
                    return false;
                }
            }
        }

        public static Type GetStorageItemTypeFromPathAsync(string path)
        {
            IStorageItem selectedStorageItem;
            try
            {
                selectedStorageItem = StorageFolder.GetFolderFromPathAsync(path).GetResults();
                return typeof(StorageFolder);
            }
            catch (Exception)
            {
                // Not a folder, so attempt to check for StorageFile
                selectedStorageItem = StorageFile.GetFileFromPathAsync(path).GetResults();
                return typeof(StorageFile);
            }
        }
    }
}
