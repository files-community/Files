using Files.Filesystem;
using GalaSoft.MvvmLight;
using System.IO;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Files.View_Models
{
    public class SelectedItemsPropertiesViewModel : ViewModelBase
    {
        private string _ItemName;

        public string ItemName
        {
            get => _ItemName;
            set => Set(ref _ItemName, value);
        }

        private string _ItemType;

        public string ItemType
        {
            get => _ItemType;
            set => Set(ref _ItemType, value);
        }

        private string _ItemPath;

        public string ItemPath
        {
            get => _ItemPath;
            set => Set(ref _ItemPath, value);
        }

        private long _ItemSizeReal;

        public long ItemSizeReal
        {
            get => _ItemSizeReal;
            set => Set(ref _ItemSizeReal, value);
        }

        private Visibility _ItemSizeProgressVisibility = Visibility.Collapsed;

        public Visibility ItemSizeProgressVisibility
        {
            get => _ItemSizeProgressVisibility;
            set => Set(ref _ItemSizeProgressVisibility, value);
        }

        private bool _SizeCalcError;

        public bool SizeCalcError
        {
            get => _SizeCalcError;
            set => Set(ref _SizeCalcError, value);
        }

        private string _ItemModifiedTimestamp;

        public string ItemModifiedTimestamp
        {
            get => _ItemModifiedTimestamp;
            set => Set(ref _ItemModifiedTimestamp, value);
        }

        private ImageSource _FileIconSource;

        public ImageSource FileIconSource
        {
            get => _FileIconSource;
            set => Set(ref _FileIconSource, value);
        }

        private bool _LoadFolderGlyph;

        public bool LoadFolderGlyph
        {
            get => _LoadFolderGlyph;
            set => Set(ref _LoadFolderGlyph, value);
        }

        private bool _LoadUnknownTypeGlyph;

        public bool LoadUnknownTypeGlyph
        {
            get => _LoadUnknownTypeGlyph;
            set => Set(ref _LoadUnknownTypeGlyph, value);
        }

        private bool _LoadFileIcon;

        public bool LoadFileIcon
        {
            get => _LoadFileIcon;
            set => Set(ref _LoadFileIcon, value);
        }

        private string _ItemsSize;

        public string ItemsSize
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
            set
            {
                if (!string.IsNullOrEmpty(value) && value != _ItemMD5Hash)
                {
                    Set(ref _ItemMD5Hash, value);
                    ItemMD5HashProgressVisibility = Visibility.Collapsed;
                }
            }
        }

        private bool _ItemMD5HashCalcError;

        public bool ItemMD5HashCalcError
        {
            get => _ItemMD5HashCalcError;
            set => Set(ref _ItemMD5HashCalcError, value);
        }

        public Visibility _ItemMD5HashVisibility = Visibility.Collapsed;

        public Visibility ItemMD5HashVisibility
        {
            get => _ItemMD5HashVisibility;
            set => Set(ref _ItemMD5HashVisibility, value);
        }

        public Visibility _ItemMD5HashProgressVisibiity = Visibility.Collapsed;

        public Visibility ItemMD5HashProgressVisibility
        {
            get => _ItemMD5HashProgressVisibiity;
            set => Set(ref _ItemMD5HashProgressVisibiity, value);
        }

        public int _FoldersCount;

        public int FoldersCount
        {
            get => _FoldersCount;
            set => Set(ref _FoldersCount, value);
        }

        public int _FilesCount;

        public int FilesCount
        {
            get => _FilesCount;
            set => Set(ref _FilesCount, value);
        }

        public string _FilesAndFoldersCountString;

        public string FilesAndFoldersCountString
        {
            get => _FilesAndFoldersCountString;
            set => Set(ref _FilesAndFoldersCountString, value);
        }

        public Visibility _FilesAndFoldersCountVisibility = Visibility.Collapsed;

        public Visibility FilesAndFoldersCountVisibility
        {
            get => _FilesAndFoldersCountVisibility;
            set => Set(ref _FilesAndFoldersCountVisibility, value);
        }

        public Microsoft.UI.Xaml.Controls.ProgressBar ItemMD5HashProgress { get; set; }

        public ListedItem Item { get; }

        public CoreDispatcher Dispatcher { get; set; }

        public SelectedItemsPropertiesViewModel(ListedItem item)
        {
            Item = item;

            ItemName = Item?.ItemName;
            ItemType = Item?.ItemType;
            ItemPath = Path.GetDirectoryName(Item?.ItemPath);
            ItemModifiedTimestamp = Item?.ItemDateModified;
            FileIconSource = Item?.FileImage;
            LoadFolderGlyph = Item != null ? Item.LoadFolderGlyph : false;
            LoadUnknownTypeGlyph = Item != null ? Item.LoadUnknownTypeGlyph : false;
            LoadFileIcon = Item != null ? Item.LoadFileIcon : false;
        }
    }
}