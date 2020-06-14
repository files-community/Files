using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Files.DataModels
{
    public class StorageItemProperties : ObservableObject
    {
        #region readonly properties
        public string ItemName
        {
            get
            {
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.ItemName;
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
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.ItemType;
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
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.ItemPath;
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
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.ItemDateModified;
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
                if (App.CurrentInstance.ContentPage == null) return null;

                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    return App.CurrentInstance.ContentPage.SelectedItem.FileImage;
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
                    return App.CurrentInstance.ContentPage.SelectedItem.LoadFolderGlyph;
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
                    return App.CurrentInstance.ContentPage.SelectedItem.LoadUnknownTypeGlyph;
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
                    return App.CurrentInstance.ContentPage.SelectedItem.LoadFileIcon;
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion
        #region Properties
        private String _ItemsSize;

        public String ItemsSize
        {
            get => _ItemsSize;
            set => Set(ref _ItemsSize, value);
        }

        private string _SelectedItemsCount;

        public string SelectedItemsCount
        {
            get => _SelectedItemsCount;
            set => Set(ref _SelectedItemsCount, value);
        }

        private bool _IsItemSelected;

        public bool IsItemSelected
        {
            get => _IsItemSelected;
            set => Set(ref _IsItemSelected, value);
        }
        private string _ItemCreatedTimestamp;

        public string ItemCreatedTimestamp
        {
            get => _ItemCreatedTimestamp;
            set => Set(ref _ItemCreatedTimestamp, value);
        }
        public string _ItemAccessedTimestamp;
        public string ItemAccessedTimestamp
        {
            get => _ItemAccessedTimestamp;
            set => Set(ref _ItemAccessedTimestamp, value);
        }
        public string _ItemFileOwner;
        public string ItemFileOwner
        {
            get => _ItemFileOwner;
            set => Set(ref _ItemFileOwner, value);
        }
        public string _ItemMD5Hash;
        public string ItemMD5Hash
        {
            get => _ItemMD5Hash;
            set => Set(ref _ItemMD5Hash, value);
        }
        public Visibility _ItemMD5HashVisibility;

        public Visibility ItemMD5HashVisibility
        {
            get => _ItemMD5HashVisibility;
            set => Set(ref _ItemMD5HashVisibility, value);
        }
        public Visibility _ItemMD5HashProgressVisibiity;

        public Visibility ItemMD5HashProgressVisibility
        {
            get => _ItemMD5HashProgressVisibiity;
            set => Set(ref _ItemMD5HashProgressVisibiity, value);
        }
        #endregion
    }
}
