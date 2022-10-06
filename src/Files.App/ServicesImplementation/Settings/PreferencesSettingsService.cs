using Files.App.Serialization;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Files.Shared.EventArguments;
using Microsoft.AppCenter.Analytics;
using System.Collections.Generic;

namespace Files.App.ServicesImplementation.Settings
{
	internal sealed class PreferencesSettingsService : BaseObservableJsonSettings, IPreferencesSettingsService
	{
		public PreferencesSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
		}

		public bool ShowConfirmDeleteDialog
		{
			get => Get(true);
			set => Set(value);
		}

		public bool OpenFoldersInNewTab
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

		public bool AreHiddenItemsVisible
		{
			get => Get(false);
			set => Set(value);
		}

		public bool AreSystemItemsHidden
		{
			get => Get(true);
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

		public bool SelectFilesOnHover
		{
			get => Get(false);
			set => Set(value);
		}

		public bool OpenFilesWithOneClick
		{
			get => Get(false);
			set => Set(value);
		}

		public bool OpenFoldersWithOneClick
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ColumnLayoutOpenFoldersWithOneClick
		{
			get => Get(true);
			set => Set(value);
		}

		public bool SearchUnindexedItems
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ForceLayoutPreferencesOnAllDirectories
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowFolderSize
		{
			get => Get(false);
			set => Set(value);
		}

		public bool OpenSpecificPageOnStartup
		{
			get => Get(false);
			set => Set(value);
		}

		public string OpenSpecificPageOnStartupPath
		{
			get => Get(string.Empty);
			set => Set(value);
		}

		public bool ContinueLastSessionOnStartUp
		{
			get => Get(false);
			set => Set(value);
		}

		public bool OpenNewTabOnStartup
		{
			get => Get(true);
			set => Set(value);
		}

		public bool AlwaysOpenNewInstance
		{
			get => Get(false);
			set => Set(value);
		}

		public List<string> TabsOnStartupList
		{
			get => Get<List<string>>(null);
			set => Set(value);
		}

		public List<string> LastSessionTabList
		{
			get => Get<List<string>>(null);
			set => Set(value);
		}

		public DateTimeFormats DateTimeFormat
		{
			get => Get(DateTimeFormats.Application);
			set => Set(value);
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

		public FolderLayoutModes DefaultLayoutMode
		{
			get => (FolderLayoutModes)Get((long)FolderLayoutModes.DetailsView);
			set => Set((long)value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(ShowConfirmDeleteDialog):
				case nameof(OpenFoldersInNewTab):
				case nameof(ShowFileExtensions):
				case nameof(AreHiddenItemsVisible):
				case nameof(AreSystemItemsHidden):
				case nameof(AreAlternateStreamsVisible):
				case nameof(ShowDotFiles):
				case nameof(SelectFilesOnHover):
				case nameof(OpenFilesWithOneClick):
				case nameof(OpenFoldersWithOneClick):
				case nameof(ColumnLayoutOpenFoldersWithOneClick):
				case nameof(SearchUnindexedItems):
				case nameof(ForceLayoutPreferencesOnAllDirectories):
				case nameof(ShowFolderSize):
				case nameof(OpenSpecificPageOnStartup):
				case nameof(ContinueLastSessionOnStartUp):
				case nameof(OpenNewTabOnStartup):
				case nameof(AlwaysOpenNewInstance):
				case nameof(ShowDateColumn):
				case nameof(ShowDateCreatedColumn):
				case nameof(ShowTypeColumn):
				case nameof(ShowSizeColumn):
				case nameof(ShowFileTagColumn):
				case nameof(DefaultLayoutMode):
					Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
					break;
			}

			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
