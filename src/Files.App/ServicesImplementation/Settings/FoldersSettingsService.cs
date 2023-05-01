// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Serialization;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Files.Shared.EventArguments;
using Microsoft.AppCenter.Analytics;

namespace Files.App.ServicesImplementation.Settings
{
	internal sealed class FoldersSettingsService : BaseObservableJsonSettings, IFoldersSettingsService
	{
		public FoldersSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
		}

		public bool SyncFolderPreferencesAcrossDirectories
		{
			get => Get(false);
			set => Set(value);
		}

		public FolderLayoutModes DefaultLayoutMode
		{
			get => (FolderLayoutModes)Get((long)FolderLayoutModes.Adaptive);
			set => Set((long)value);
		}

		public double TagColumnWidth
		{
			get => Get(140d);
			set
			{
				if (ShowFileTagColumn)
					Set(value);
			}
		}

		public double NameColumnWidth
		{
			get => Get(240d);
			set => Set(value);
		}

		public double DateModifiedColumnWidth
		{
			get => Get(200d);
			set
			{
				if (ShowDateColumn)
					Set(value);
			}
		}

		public double TypeColumnWidth
		{
			get => Get(140d);
			set
			{
				if (ShowTypeColumn)
					Set(value);
			}
		}

		public double DateCreatedColumnWidth
		{
			get => Get(200d);
			set
			{
				if (ShowDateCreatedColumn)
					Set(value);
			}
		}

		public double SizeColumnWidth
		{
			get => Get(140d);
			set
			{
				if (ShowSizeColumn)
					Set(value);
			}
		}

		public double DateDeletedColumnWidth
		{
			get => Get(200d);
			set
			{
				if (ShowDateDeletedColumn)
					Set(value);
			}
		}

		public double OriginalPathColumnWidth
		{
			get => Get(200d);
			set
			{
				if (ShowOriginalPathColumn)
					Set(value);
			}
		}

		public double SyncStatusColumnWidth
		{
			get => Get(50d);
			set
			{
				if (ShowSyncStatusColumn)
					Set(value);
			}
		}

		public bool ShowDateColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowDateCreatedColumn
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowTypeColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowSizeColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowFileTagColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowDateDeletedColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowOriginalPathColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowSyncStatusColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowHiddenItems
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowProtectedSystemFiles
		{
			get => Get(false);
			set => Set(value);
		}

		public bool AreAlternateStreamsVisible
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowDotFiles
		{
			get => Get(true);
			set => Set(value);
		}

		public bool OpenItemsWithOneClick
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ColumnLayoutOpenFoldersWithOneClick
		{
			get => Get(true);
			set => Set(value);
		}

		public bool OpenFoldersInNewTab
		{
			get => Get(false);
			set => Set(value);
		}

		public bool CalculateFolderSizes
		{
			get => Get(false);
			set => Set(value);
		}

		public SortOption DefaultSortOption
		{
			get => (SortOption)Get((long)SortOption.Name);
			set => Set((long)value);
		}

		public GroupOption DefaultGroupOption
		{
			get => (GroupOption)Get((long)GroupOption.None);
			set => Set((long)value);
		}

		public SortDirection DefaultDirectorySortDirection
		{
			get => (SortDirection)Get((long)SortDirection.Ascending);
			set => Set((long)value);
		}

		public SortDirection DefaultDirectoryGroupDirection
		{
			get => (SortDirection)Get((long)SortDirection.Ascending);
			set => Set((long)value);
		}

		public bool DefaultSortDirectoriesAlongsideFiles
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowFileExtensions
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowThumbnails
		{
			get => Get(true);
			set => Set(value);
		}

		public DeleteConfirmationPolicies DeleteConfirmationPolicy
		{
			get => (DeleteConfirmationPolicies)Get((long)DeleteConfirmationPolicies.Always);
			set => Set((long)value);
		}

		public bool SelectFilesOnHover
		{
			get => Get(false);
			set => Set(value);
		}

		public bool DoubleClickToGoUp
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowFileExtensionWarning
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowCheckboxesWhenSelectingItems
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowCheckboxes
		{
			get => Get(true);
			set => Set(value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(SyncFolderPreferencesAcrossDirectories):
				case nameof(DefaultLayoutMode):
				case nameof(TagColumnWidth):
				case nameof(NameColumnWidth):
				case nameof(DateModifiedColumnWidth):
				case nameof(TypeColumnWidth):
				case nameof(DateCreatedColumnWidth):
				case nameof(SizeColumnWidth):
				case nameof(ShowDateColumn):
				case nameof(ShowDateCreatedColumn):
				case nameof(ShowTypeColumn):
				case nameof(ShowSizeColumn):
				case nameof(ShowFileTagColumn):
				case nameof(ShowHiddenItems):
				case nameof(ShowProtectedSystemFiles):
				case nameof(AreAlternateStreamsVisible):
				case nameof(ShowDotFiles):
				case nameof(OpenItemsWithOneClick):
				case nameof(ColumnLayoutOpenFoldersWithOneClick):
				case nameof(OpenFoldersInNewTab):
				case nameof(CalculateFolderSizes):
				case nameof(ShowFileExtensions):
				case nameof(ShowThumbnails):
				case nameof(DeleteConfirmationPolicy):
				case nameof(SelectFilesOnHover):
				case nameof(DoubleClickToGoUp):
				case nameof(ShowFileExtensionWarning):
				case nameof(ShowCheckboxesWhenSelectingItems):
					Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
					break;
			}

			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
