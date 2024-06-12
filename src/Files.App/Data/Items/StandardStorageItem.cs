// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Properties;
using Files.Shared.Helpers;
using FluentFTP;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Drawing;
using System.IO;
using System.Text;
using Windows.Storage;

namespace Files.App.Utils
{
	/// <summary>
	/// Represents storage item to be displayed on UI with necessary storage properties.
	/// </summary>
	// TODO: Add required keyword
	public class StandardStorageItem : ObservableObject, IGroupableItem
	{
		// Dependency injections

		protected IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		protected IDateTimeFormatter DateTimeFormatter { get; } = Ioc.Default.GetRequiredService<IDateTimeFormatter>();
		protected IStartMenuService StartMenuService { get; } = Ioc.Default.GetRequiredService<IStartMenuService>();

		// Properties

		/// <summary>
		/// Gets type of this storage item.
		/// </summary>
		public StorableKind StorableKind { get; }

		public bool IsHiddenItem { get; set; }

		[Obsolete("Do not use furthermore. Use StorableKind instead.")]
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
				tooltipBuilder.AppendLine($"{"NameWithColon".GetLocalizedResource()} {Name}");
				tooltipBuilder.AppendLine($"{"ItemType".GetLocalizedResource()} {itemType}");
				tooltipBuilder.Append($"{"ToolTipDescriptionDate".GetLocalizedResource()} {ItemDateModified}");
				if (!string.IsNullOrWhiteSpace(FileSize))
					tooltipBuilder.Append($"{Environment.NewLine}{"SizeLabel".GetLocalizedResource()} {FileSize}");
				if (!string.IsNullOrWhiteSpace(DimensionsDisplay))
					tooltipBuilder.Append($"{Environment.NewLine}{"PropertyDimensions".GetLocalizedResource()}: {DimensionsDisplay}");
				if (SyncStatusUI.LoadSyncStatus)
					tooltipBuilder.Append($"{Environment.NewLine}{"syncStatusColumn/Header".GetLocalizedResource()}: {syncStatusUI.SyncStatusString}");

				return tooltipBuilder.ToString();
			}
		}

		public string FolderRelativeId { get; set; }

		public bool HasChildren { get; set; } = true;

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

		private string[] fileTags = null!;
		public string[] FileTags
		{
			get => fileTags;
			set
			{
				// fileTags is null when the item is first created
				var fileTagsInitialized = fileTags is not null;
				if (SetProperty(ref fileTags, value))
				{
					Debug.Assert(value != null);

					// only set the tags if the file tags have been changed
					if (fileTagsInitialized)
					{
						var dbInstance = FileTagsHelper.GetDbInstance();
						dbInstance.SetTags(ItemPath, FileFRN, value);
						FileTagsHelper.WriteFileTag(ItemPath, value);
					}

					HasTags = !FileTags.IsEmpty();
					OnPropertyChanged(nameof(FileTagsUI));
				}
			}
		}

		public IList<TagViewModel>? FileTagsUI
		{
			get => FileTagsSettingsService.GetTagsByIds(FileTags);
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
				if (SetProperty(ref fileImage, value))
				{
					if (value is BitmapImage)
					{
						LoadFileIcon = true;
						NeedsPlaceholderGlyph = false;
					}
				}
			}
		}

		public bool IsItemPinnedToStart => StartMenuService.IsPinned((this as ShortcutItem)?.TargetPath ?? ItemPath);

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
				ItemDateModified = DateTimeFormatter.ToShortLabel(value);
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
				ItemDateCreated = DateTimeFormatter.ToShortLabel(value);
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
				ItemDateAccessed = DateTimeFormatter.ToShortLabel(value);
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

		public string DimensionsDisplay
		{
			get
			{
				int imageHeight = 0;
				int imageWidth = 0;

				var isImageFile = FileExtensionHelpers.IsImageFile(FileExtension);
				if (isImageFile)
				{
					try
					{
						// TODO: Consider to use 'System.Kind' instead.
						using FileStream fileStream = new(ItemPath, FileMode.Open, FileAccess.Read, FileShare.Read);
						using Image image = Image.FromStream(fileStream, false, false);

						if (image is not null)
						{
							imageHeight = image.Height;
							imageWidth = image.Width;
						}
					}
					catch { }
				}


				return
					isImageFile &&
					imageWidth > 0 &&
					imageHeight > 0
						? $"{imageWidth} \u00D7 {imageHeight}"
						: string.Empty;
			}
		}

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
		public bool IsRecycleBinItem => this is StandardRecycleBinItem;
		public bool IsShortcut => this is ShortcutItem;
		public bool IsLibrary => this is LibraryItem;
		public bool IsLinkItem => IsShortcut && ((ShortcutItem)this).IsUrl;
		public bool IsFtpItem => this is StandardFtpItem;
		public bool IsArchive => this is ZipItem;
		public bool IsAlternateStream => this is AlternateStreamItem;
		public bool IsGitItem => this is GitItem;
		public virtual bool IsExecutable => FileExtensionHelpers.IsExecutableFile(ItemPath);
		public virtual bool IsScriptFile => FileExtensionHelpers.IsScriptFile(ItemPath);
		public bool IsPinned => App.QuickAccessManager.Model.PinnedFolders.Contains(itemPath);
		public bool IsDriveRoot => ItemPath == PathNormalization.GetPathRoot(ItemPath);
		public bool IsElevationRequired { get; set; }

		private BaseStorageFile itemFile;
		public BaseStorageFile ItemFile
		{
			get => itemFile;
			set => SetProperty(ref itemFile, value);
		}

		// This is a hack used because x:Bind casting did not work properly
		public StandardRecycleBinItem AsRecycleBinItem => this as StandardRecycleBinItem;

		public GitItem AsGitItem => this as GitItem;

		public string Key { get; set; }

		// Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="StandardStorageItem" /> class.
		/// </summary>
		public StandardStorageItem()
		{
		}

		// Methods

		/// <summary>
		/// Manually check if a folder path contains child items,
		/// updating the ContainsFilesOrFolders property from its default value of true
		/// </summary>
		public void UpdateContainsFilesFolders()
		{
			HasChildren = FolderHelpers.CheckForFilesFolders(ItemPath);
		}
	}

	public sealed class ShortcutItem : StandardStorageItem
	{
		public ShortcutItem(string folderRelativeId) : base()
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

	public sealed class ZipItem : StandardStorageItem
	{
		public ZipItem(string folderRelativeId) : base()
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

	public sealed class LibraryItem : StandardStorageItem
	{
		public LibraryItem(LibraryLocationItem library) : base()
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

	public sealed class AlternateStreamItem : StandardStorageItem
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

	public sealed class GitItem : StandardStorageItem
	{
		private volatile int statusPropertiesInitialized = 0;
		public bool StatusPropertiesInitialized
		{
			get => statusPropertiesInitialized == 1;
			set => Interlocked.Exchange(ref statusPropertiesInitialized, value ? 1 : 0);
		}

		private volatile int commitPropertiesInitialized = 0;
		public bool CommitPropertiesInitialized
		{
			get => commitPropertiesInitialized == 1;
			set => Interlocked.Exchange(ref commitPropertiesInitialized, value ? 1 : 0);
		}

		private Style? _UnmergedGitStatusIcon;
		public Style? UnmergedGitStatusIcon
		{
			get => _UnmergedGitStatusIcon;
			set => SetProperty(ref _UnmergedGitStatusIcon, value);
		}

		private string? _UnmergedGitStatusName;
		public string? UnmergedGitStatusName
		{
			get => _UnmergedGitStatusName;
			set => SetProperty(ref _UnmergedGitStatusName, value);
		}

		private DateTimeOffset? _GitLastCommitDate;
		public DateTimeOffset? GitLastCommitDate
		{
			get => _GitLastCommitDate;
			set
			{
				SetProperty(ref _GitLastCommitDate, value);
				GitLastCommitDateHumanized = value is DateTimeOffset dto ? DateTimeFormatter.ToShortLabel(dto) : "";
			}
		}

		private string? _GitLastCommitDateHumanized;
		public string? GitLastCommitDateHumanized
		{
			get => _GitLastCommitDateHumanized;
			set => SetProperty(ref _GitLastCommitDateHumanized, value);
		}

		private string? _GitLastCommitMessage;
		public string? GitLastCommitMessage
		{
			get => _GitLastCommitMessage;
			set => SetProperty(ref _GitLastCommitMessage, value);
		}

		private string? _GitCommitAuthor;
		public string? GitLastCommitAuthor
		{
			get => _GitCommitAuthor;
			set => SetProperty(ref _GitCommitAuthor, value);
		}

		private string? _GitLastCommitSha;
		public string? GitLastCommitSha
		{
			get => _GitLastCommitSha;
			set => SetProperty(ref _GitLastCommitSha, value);
		}

		private string? _GitLastCommitFullSha;
		public string? GitLastCommitFullSha
		{
			get => _GitLastCommitFullSha;
			set => SetProperty(ref _GitLastCommitFullSha, value);
		}
	}
}
