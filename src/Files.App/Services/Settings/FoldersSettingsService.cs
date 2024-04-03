// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Server.Data.Enums;
using Microsoft.AppCenter.Analytics;

namespace Files.App.Services.Settings
{
	internal sealed class FoldersSettingsService : BaseObservableJsonSettings, IFoldersSettingsService
	{
		public FoldersSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
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

		public bool ScrollToPreviousFolderWhenNavigatingUp
		{
			get => Get(true);
			set => Set(value);
		}

		public bool CalculateFolderSizes
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

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(ShowHiddenItems):
				case nameof(ShowProtectedSystemFiles):
				case nameof(AreAlternateStreamsVisible):
				case nameof(ShowDotFiles):
				case nameof(OpenItemsWithOneClick):
				case nameof(ColumnLayoutOpenFoldersWithOneClick):
				case nameof(OpenFoldersInNewTab):
				case nameof(ScrollToPreviousFolderWhenNavigatingUp):
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
