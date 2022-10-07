using Files.App.Serialization;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Files.Shared.EventArguments;
using Microsoft.AppCenter.Analytics;
using System;

namespace Files.App.ServicesImplementation.Settings
{
    internal sealed class FoldersSettingsService : BaseObservableJsonSettings, IFoldersSettingsService
	{
        public FoldersSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Register root
            RegisterSettingsContext(settingsSharingContext);
        }

		public bool EnableOverridingFolderPreferences
		{
			get => Get(true);
			set => Set(value);
		}

		public FolderLayoutModes DefaultLayoutMode
		{
			get => (FolderLayoutModes)Get((long)FolderLayoutModes.DetailsView);
			set => Set((long)value);
		}

		public double TagColumnWidth
		{
			get => Get(200d);
			set => Set(value);
		}

		public double NameColumnWidth
		{
			get => Get(200d);
			set => Set(value);
		}

		public double DateModifiedColumnWidth
		{
			get => Get(200d);
			set => Set(value);
		}

		public double TypeColumnWidth
		{
			get => Get(200d);
			set => Set(value);
		}

		public double DateCreatedColumnWidth
		{
			get => Get(200d);
			set => Set(value);
		}

		public double SizeColumnWidth
		{
			get => Get(200d);
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

		public bool OpenFoldersInNewTab
		{
			get => Get(false);
			set => Set(value);
		}
		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            switch (e.SettingName)
            {
                case nameof(EnableOverridingFolderPreferences):
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
				case nameof(OpenFilesWithOneClick):
				case nameof(OpenFoldersWithOneClick):
				case nameof(ColumnLayoutOpenFoldersWithOneClick):
				case nameof(OpenFoldersInNewTab):
					Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
                    break;
            }

            base.RaiseOnSettingChangedEvent(sender, e);
        }
    }
}
