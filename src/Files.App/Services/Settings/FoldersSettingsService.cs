// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services.Settings
{
	internal sealed partial class FoldersSettingsService : BaseObservableJsonSettings, IFoldersSettingsService
	{
		public FoldersSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
			MigrateLegacySingleClickSettings();
		}

		// Migrates the pre-SingleClickOpenMode settings (`OpenItemsWithOneClick` as bool and `OpenFoldersWithOneClick`
		// as the legacy 3-value enum) into the new mode-based properties. Runs once per install — the legacy keys
		// are removed after migration so subsequent launches no-op.
		private void MigrateLegacySingleClickSettings()
		{
			if (JsonSettingsDatabase?.ExportSettings() is not IDictionary<string, object?> data)
				return;

			if (data.ContainsKey("OpenItemsWithOneClick"))
			{
				var legacy = JsonSettingsDatabase.GetValue<bool>("OpenItemsWithOneClick");
				OpenFilesWithSingleClick = legacy ? SingleClickOpenMode.Always : SingleClickOpenMode.Never;
				JsonSettingsDatabase.RemoveKey("OpenItemsWithOneClick");
			}

			if (data.ContainsKey("OpenFoldersWithOneClick"))
			{
				// Legacy values: 0 = OnlyInColumnsView, 1 = Always, 2 = Never
				var legacy = JsonSettingsDatabase.GetValue<int>("OpenFoldersWithOneClick");
				switch (legacy)
				{
					case 0:
						OpenFoldersWithSingleClick = SingleClickOpenMode.Never;
						OpenFoldersInColumnsViewWithSingleClick = SingleClickOpenMode.Always;
						break;
					case 1:
						OpenFoldersWithSingleClick = SingleClickOpenMode.Always;
						OpenFoldersInColumnsViewWithSingleClick = SingleClickOpenMode.Always;
						break;
					case 2:
						OpenFoldersWithSingleClick = SingleClickOpenMode.Never;
						OpenFoldersInColumnsViewWithSingleClick = SingleClickOpenMode.Never;
						break;
				}
				JsonSettingsDatabase.RemoveKey("OpenFoldersWithOneClick");
			}
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

		/// <inheritdoc/>
		public SingleClickOpenMode OpenFilesWithSingleClick
		{
			get => Get(SingleClickOpenMode.OnlyForTouch);
			set => Set(value);
		}

		/// <inheritdoc/>
		public SingleClickOpenMode OpenFoldersWithSingleClick
		{
			get => Get(SingleClickOpenMode.OnlyForTouch);
			set => Set(value);
		}

		/// <inheritdoc/>
		public SingleClickOpenMode OpenFoldersInColumnsViewWithSingleClick
		{
			get => Get(SingleClickOpenMode.Always);
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

		/// <inheritdoc/>
		public SizeUnitTypes SizeUnitFormat
		{
			get => Get(SizeUnitTypes.BinaryUnits);
			set => Set(value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
