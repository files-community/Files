using Files.App.Filesystem.Cloud;
using Files.App.Filesystem.StorageItems;
using Files.App.ViewModels.Properties;
using Files.Backend.Helpers;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.FileTags;
using Files.Shared.Extensions;
using Files.Shared.Services.DateTimeFormatter;
using FluentFTP;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Files.App.Filesystem
{
	public class ListedItem : ObservableObject, IGroupableItem
	{
		protected static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		protected static readonly IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

		protected static readonly IDateTimeFormatter dateTimeFormatter = Ioc.Default.GetRequiredService<IDateTimeFormatter>();

		public bool IsHiddenItem { get; set; } = false;

		public StorageItemTypes PrimaryItemAttribute { get; set; }

		private volatile int itemPropertiesInitialized = 0;
		public bool ItemPropertiesInitialized
		{
			get => itemPropertiesInitialized == 1;
			set => Interlocked.Exchange(ref itemPropertiesInitialized, value ? 1 : 0);
		}

		public string ItemTooltipText
		{
			get
			{
				var tooltipBuilder = new StringBuilder();
				tooltipBuilder.AppendLine($"{"ToolTipDescriptionName".GetLocalizedResource()} {Name}");
				tooltipBuilder.AppendLine($"{"ToolTipDescriptionType".GetLocalizedResource()} {itemType}");
				tooltipBuilder.Append($"{"ToolTipDescriptionDate".GetLocalizedResource()} {ItemDateModified}");
				if(!string.IsNullOrWhiteSpace(FileSize))
					tooltipBuilder.Append($"{Environment.NewLine}{"SizeLabel".GetLocalizedResource()} {FileSize}");
				if(SyncStatusUI.LoadSyncStatus)
					tooltipBuilder.Append($"{Environment.NewLine}{"syncStatusColumn/Header".GetLocalizedResource()}: {syncStatusUI.SyncStatusString}");

				return tooltipBuilder.ToString();
			}
		}

		public string FolderRelativeId { get; set; }

		public bool ContainsFilesOrFolders { get; set; } = true;

		private bool needsPlaceholderGlyph = true;
		public bool NeedsPlaceholderGlyph
		{
			get => needsPlaceholderGlyph;
			set => SetProperty(ref needsPlaceholderGlyph, value);
		}

		private bool loadFileIcon;
		public bool LoadFileIcon
		{
			get => loadFileIcon;
			set => SetProperty(ref loadFileIcon, value);
		}

		private bool loadDefaultIcon = false;
		public bool LoadDefaultIcon
		{
			get => loadDefaultIcon;
			[Obsolete("The set accessor is used internally and should not be used outside ListedItem and derived classes.")]
			set => SetProperty(ref loadDefaultIcon, value);
		}

		private bool loadWebShortcutGlyph;
		public bool LoadWebShortcutGlyph
		{
			get => loadWebShortcutGlyph;
			set
			{
				if (SetProperty(ref loadWebShortcutGlyph, value))
				{
					LoadDefaultIcon = !value;
				}
			}
		}

		private bool loadCustomIcon;
		public bool LoadCustomIcon
		{
			get => loadCustomIcon;
			set => SetProperty(ref loadCustomIcon, value);
		}

		// Note: Never attempt to call this from a secondary window or another thread, create a new instance from CustomIconSource instead
		// TODO: eventually we should remove this b/c it's not thread safe
		private BitmapImage customIcon;
		public BitmapImage CustomIcon
		{
			get => customIcon;
			set
			{
				LoadCustomIcon = true;
				SetProperty(ref customIcon, value);
			}
		}

		public ulong? FileFRN { get; set; }

		private string[] fileTags; // TODO: initialize to empty array after UI is done
		public string[] FileTags
		{
			get => fileTags;
			set
			{
				if (SetProperty(ref fileTags, value))
				{
					var dbInstance = FileTagsHelper.GetDbInstance();
					dbInstance.SetTags(ItemPath, FileFRN, value);
					HasTags = !FileTags.IsEmpty();
					FileTagsHelper.WriteFileTag(ItemPath, value);
					OnPropertyChanged(nameof(FileTagsUI));
				}
			}
		}

		public IList<TagViewModel> FileTagsUI
		{
			get => fileTagsSettingsService.GetTagsByIds(FileTags);
		}

		private Uri customIconSource;
		public Uri CustomIconSource
		{
			get => customIconSource;
			set => SetProperty(ref customIconSource, value);
		}

		private double opacity;
		public double Opacity
		{
			get => opacity;
			set => SetProperty(ref opacity, value);
		}

		private bool hasTags;
		public bool HasTags
		{
			get => hasTags;
			set => SetProperty(ref hasTags, value);
		}

		private CloudDriveSyncStatusUI syncStatusUI = new();
		public CloudDriveSyncStatusUI SyncStatusUI
		{
			get => syncStatusUI;
			set
			{
				// For some reason this being null will cause a crash with bindings
				value ??= new CloudDriveSyncStatusUI();
				if (SetProperty(ref syncStatusUI, value))
				{
					OnPropertyChanged(nameof(SyncStatusString));
					OnPropertyChanged(nameof(ItemTooltipText));
				}
			}
		}

		// This is used to avoid passing a null value to AutomationProperties.Name, which causes a crash
		public string SyncStatusString
		{
			get => string.IsNullOrEmpty(SyncStatusUI?.SyncStatusString) ? "CloudDriveSyncStatus_Unknown".GetLocalizedResource() : SyncStatusUI.SyncStatusString;
		}

		private BitmapImage fileImage;
		public BitmapImage FileImage
		{
			get => fileImage;
			set
			{
				if (fileImage is BitmapImage imgOld)
				{
					imgOld.ImageOpened -= Img_ImageOpened;
				}
				if (SetProperty(ref fileImage, value))
				{
					if (value is BitmapImage img)
					{
						if (img.PixelWidth > 0)
						{
							Img_ImageOpened(img, null);
						}
						else
						{
							img.ImageOpened += Img_ImageOpened;
						}
					}
				}
			}
		}

		private void Img_ImageOpened(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			if (sender is BitmapImage image)
			{
				image.ImageOpened -= Img_ImageOpened;

				if (image.PixelWidth > 0)
				{
					SafetyExtensions.IgnoreExceptions(() =>
					{
						LoadFileIcon = true;
						PlaceholderDefaultIcon = null;
						NeedsPlaceholderGlyph = false;
						LoadDefaultIcon = false;
						LoadWebShortcutGlyph = false;
					}, App.Logger); // 2009482836u
				}
			}
		}

		public bool IsItemPinnedToStart => App.SecondaryTileHelper.CheckFolderPinned(ItemPath);

		private BitmapImage iconOverlay;
		public BitmapImage IconOverlay
		{
			get => iconOverlay;
			set
			{
				if (value is not null)
				{
					SetProperty(ref iconOverlay, value);
				}
			}
		}

		private BitmapImage placeholderDefaultIcon;
		public BitmapImage PlaceholderDefaultIcon
		{
			get => placeholderDefaultIcon;
			set => SetProperty(ref placeholderDefaultIcon, value);
		}

		private BitmapImage shieldIcon;
		public BitmapImage ShieldIcon
		{
			get => shieldIcon;
			set
			{
				if (value is not null)
				{
					SetProperty(ref shieldIcon, value);
				}
			}
		}

		private string itemPath;
		public string ItemPath
		{
			get => itemPath;
			set => SetProperty(ref itemPath, value);
		}

		private string itemNameRaw;
		public string ItemNameRaw
		{
			get => itemNameRaw;
			set
			{
				if (SetProperty(ref itemNameRaw, value))
				{
					OnPropertyChanged(nameof(Name));
				}
			}
		}

		public virtual string Name
		{
			get
			{
				if (PrimaryItemAttribute == StorageItemTypes.File)
				{
					var nameWithoutExtension = Path.GetFileNameWithoutExtension(itemNameRaw);
					if (!string.IsNullOrEmpty(nameWithoutExtension) && !UserSettingsService.FoldersSettingsService.ShowFileExtensions)
					{
						return nameWithoutExtension;
					}
				}
				return itemNameRaw;
			}
		}

		private string itemType;
		public string ItemType
		{
			get => itemType;
			set
			{
				if (value is not null)
				{
					SetProperty(ref itemType, value);
				}
			}
		}

		public string FileExtension { get; set; }

		private string fileSize;
		public string FileSize
		{
			get => fileSize;
			set
			{
				SetProperty(ref fileSize, value);
				OnPropertyChanged(nameof(FileSizeDisplay));
			}
		}

		public string FileSizeDisplay => string.IsNullOrEmpty(FileSize) ? "ItemSizeNotCalculated".GetLocalizedResource() : FileSize;

		public long FileSizeBytes { get; set; }

		public string ItemDateModified { get; private set; }

		public string ItemDateCreated { get; private set; }

		public string ItemDateAccessed { get; private set; }

		private DateTimeOffset itemDateModifiedReal;
		public DateTimeOffset ItemDateModifiedReal
		{
			get => itemDateModifiedReal;
			set
			{
				ItemDateModified = dateTimeFormatter.ToShortLabel(value);
				itemDateModifiedReal = value;
				OnPropertyChanged(nameof(ItemDateModified));
			}
		}

		private DateTimeOffset itemDateCreatedReal;
		public DateTimeOffset ItemDateCreatedReal
		{
			get => itemDateCreatedReal;
			set
			{
				ItemDateCreated = dateTimeFormatter.ToShortLabel(value);
				itemDateCreatedReal = value;
				OnPropertyChanged(nameof(ItemDateCreated));
			}
		}

		private DateTimeOffset itemDateAccessedReal;
		public DateTimeOffset ItemDateAccessedReal
		{
			get => itemDateAccessedReal;
			set
			{
				ItemDateAccessed = dateTimeFormatter.ToShortLabel(value);
				itemDateAccessedReal = value;
				OnPropertyChanged(nameof(ItemDateAccessed));
			}
		}

		private ObservableCollection<FileProperty> itemProperties;
		public ObservableCollection<FileProperty> ItemProperties
		{
			get => itemProperties;
			set => SetProperty(ref itemProperties, value);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ListedItem" /> class.
		/// </summary>
		/// <param name="folderRelativeId"></param>
		public ListedItem(string folderRelativeId) => FolderRelativeId = folderRelativeId;

		// Parameterless constructor for JsonConvert
		public ListedItem() { }

		private ObservableCollection<FileProperty> fileDetails;
		public ObservableCollection<FileProperty> FileDetails
		{
			get => fileDetails;
			set => SetProperty(ref fileDetails, value);
		}

		public override string ToString()
		{
			string suffix;
			if (IsRecycleBinItem)
			{
				suffix = "RecycleBinItemAutomation".GetLocalizedResource();
			}
			else if (IsShortcut)
			{
				suffix = "ShortcutItemAutomation".GetLocalizedResource();
			}
			else if (IsLibrary)
			{
				suffix = "Library".GetLocalizedResource();
			}
			else
			{
				suffix = PrimaryItemAttribute == StorageItemTypes.File ? "Folder".GetLocalizedResource() : "FolderItemAutomation".GetLocalizedResource();
			}

			return $"{Name}, {suffix}";
		}

		public bool IsFolder => PrimaryItemAttribute is StorageItemTypes.Folder;
		public bool IsRecycleBinItem => this is RecycleBinItem;
		public bool IsShortcut => this is ShortcutItem;
		public bool IsLibrary => this is LibraryItem;
		public bool IsLinkItem => IsShortcut && ((ShortcutItem)this).IsUrl;
		public bool IsFtpItem => this is FtpItem;
		public bool IsArchive => this is ZipItem;
		public bool IsAlternateStream => this is AlternateStreamItem;
		public virtual bool IsExecutable => FileExtensionHelpers.IsExecutableFile(ItemPath);
		public bool IsPinned => App.QuickAccessManager.Model.FavoriteItems.Contains(itemPath);
		public bool IsDriveRoot => ItemPath == PathNormalization.GetPathRoot(ItemPath);
		public bool IsElevated => CheckElevationRights();

		private BaseStorageFile itemFile;
		public BaseStorageFile ItemFile
		{
			get => itemFile;
			set => SetProperty(ref itemFile, value);
		}

		// This is a hack used because x:Bind casting did not work properly
		public RecycleBinItem AsRecycleBinItem => this as RecycleBinItem;

		public string Key { get; set; }

		/// <summary>
		/// Manually check if a folder path contains child items,
		/// updating the ContainsFilesOrFolders property from its default value of true
		/// </summary>
		public void UpdateContainsFilesFolders()
		{
			ContainsFilesOrFolders = FolderHelpers.CheckForFilesFolders(ItemPath);
		}

		public void SetDefaultIcon(BitmapImage img)
		{
			NeedsPlaceholderGlyph = false;
			LoadDefaultIcon = true;
			PlaceholderDefaultIcon = img;
		}

		private bool CheckElevationRights()
		{
			return IsShortcut
				? ElevationHelpers.IsElevationRequired(((ShortcutItem)this).TargetPath)
				: ElevationHelpers.IsElevationRequired(this.ItemPath);
		}
	}

	public class RecycleBinItem : ListedItem
	{
		public RecycleBinItem(string folderRelativeId) : base(folderRelativeId)
		{
		}

		public string ItemDateDeleted { get; private set; }

		public DateTimeOffset ItemDateDeletedReal
		{
			get => itemDateDeletedReal;
			set
			{
				ItemDateDeleted = dateTimeFormatter.ToShortLabel(value);
				itemDateDeletedReal = value;
			}
		}

		private DateTimeOffset itemDateDeletedReal;

		// For recycle bin elements (path + name)
		public string ItemOriginalPath { get; set; }

		// For recycle bin elements (path)
		public string ItemOriginalFolder => Path.IsPathRooted(ItemOriginalPath) ? Path.GetDirectoryName(ItemOriginalPath) : ItemOriginalPath;

		public string ItemOriginalFolderName => Path.GetFileName(ItemOriginalFolder);
	}

	public class FtpItem : ListedItem
	{
		public FtpItem(FtpListItem item, string folder) : base(null)
		{
			var isFile = item.Type == FtpObjectType.File;
			ItemDateCreatedReal = item.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawCreated;
			ItemDateModifiedReal = item.RawModified < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawModified;
			ItemNameRaw = item.Name;
			FileExtension = Path.GetExtension(item.Name);
			ItemPath = PathNormalization.Combine(folder, item.Name);
			PrimaryItemAttribute = isFile ? StorageItemTypes.File : StorageItemTypes.Folder;
			ItemPropertiesInitialized = false;

			var itemType = isFile ? "File".GetLocalizedResource() : "Folder".GetLocalizedResource();
			if (isFile && Name.Contains('.', StringComparison.Ordinal))
			{
				itemType = FileExtension.Trim('.') + " " + itemType;
			}

			ItemType = itemType;
			FileSizeBytes = item.Size;
			ContainsFilesOrFolders = !isFile;
			FileImage = null;
			FileSize = isFile ? FileSizeBytes.ToSizeString() : null;
			Opacity = 1;
			IsHiddenItem = false;
		}

		public async Task<IStorageItem> ToStorageItem() => PrimaryItemAttribute switch
		{
			StorageItemTypes.File => await new FtpStorageFile(ItemPath, ItemNameRaw, ItemDateCreatedReal).ToStorageFileAsync(),
			StorageItemTypes.Folder => new FtpStorageFolder(ItemPath, ItemNameRaw, ItemDateCreatedReal),
			_ => throw new InvalidDataException(),
		};
	}

	public class ShortcutItem : ListedItem
	{
		public ShortcutItem(string folderRelativeId) : base(folderRelativeId)
		{
		}

		// Parameterless constructor for JsonConvert
		public ShortcutItem() : base()
		{ }

		// For shortcut elements (.lnk and .url)
		public string TargetPath { get; set; }

		public override string Name
			=> IsSymLink ? base.Name : Path.GetFileNameWithoutExtension(ItemNameRaw); // Always hide extension for shortcuts

		public string Arguments { get; set; }
		public string WorkingDirectory { get; set; }
		public bool RunAsAdmin { get; set; }
		public bool IsUrl { get; set; }
		public bool IsSymLink { get; set; }
		public override bool IsExecutable => FileExtensionHelpers.IsExecutableFile(TargetPath, true);
	}

	public class ZipItem : ListedItem
	{
		public ZipItem(string folderRelativeId) : base(folderRelativeId)
		{
		}

		public override string Name
		{
			get
			{
				var nameWithoutExtension = Path.GetFileNameWithoutExtension(ItemNameRaw);
				if (!string.IsNullOrEmpty(nameWithoutExtension) && !UserSettingsService.FoldersSettingsService.ShowFileExtensions)
				{
					return nameWithoutExtension;
				}
				return ItemNameRaw;
			}
		}

		// Parameterless constructor for JsonConvert
		public ZipItem() : base()
		{ }
	}

	public class LibraryItem : ListedItem
	{
		public LibraryItem(LibraryLocationItem library) : base(null)
		{
			ItemPath = library.Path;
			ItemNameRaw = library.Text;
			PrimaryItemAttribute = StorageItemTypes.Folder;
			ItemType = "Library".GetLocalizedResource();
			LoadCustomIcon = true;
			CustomIcon = library.Icon;
			//CustomIconSource = library.IconSource;
			LoadFileIcon = true;

			IsEmpty = library.IsEmpty;
			DefaultSaveFolder = library.DefaultSaveFolder;
			Folders = library.Folders;
		}

		public bool IsEmpty { get; }

		public string DefaultSaveFolder { get; }

		public override string Name => ItemNameRaw;

		public ReadOnlyCollection<string> Folders { get; }
	}

	public class AlternateStreamItem : ListedItem
	{
		public string MainStreamPath => ItemPath.Substring(0, ItemPath.LastIndexOf(':'));
		public string MainStreamName => Path.GetFileName(MainStreamPath);

		public override string Name
		{
			get
			{
				var nameWithoutExtension = Path.GetFileNameWithoutExtension(ItemNameRaw);
				var mainStreamNameWithoutExtension = Path.GetFileNameWithoutExtension(MainStreamName);
				if (!UserSettingsService.FoldersSettingsService.ShowFileExtensions)
				{
					return $"{(string.IsNullOrEmpty(mainStreamNameWithoutExtension) ? MainStreamName : mainStreamNameWithoutExtension)}:{(string.IsNullOrEmpty(nameWithoutExtension) ? ItemNameRaw : nameWithoutExtension)}";
				}
				return $"{MainStreamName}:{ItemNameRaw}";
			}
		}
	}
}