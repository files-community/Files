// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Settings;

public sealed partial class Settings
{
	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowHiddenItems { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowProtectedSystemFiles { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool AreAlternateStreamsVisible { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowDotFiles { get; set; }

	[GeneratedSettingsProperty(DefaultValue = SingleClickOpenMode.OnlyForTouch)]
	public partial SingleClickOpenMode OpenFilesWithSingleClick { get; set; }

	[GeneratedSettingsProperty(DefaultValue = SingleClickOpenMode.OnlyForTouch)]
	public partial SingleClickOpenMode OpenFoldersWithSingleClick { get; set; }

	[GeneratedSettingsProperty(DefaultValue = SingleClickOpenMode.Always)]
	public partial SingleClickOpenMode OpenFoldersInColumnsViewWithSingleClick { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool OpenFoldersInNewTab { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ScrollToPreviousFolderWhenNavigatingUp { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool CalculateFolderSizes { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowFileExtensions { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowThumbnails { get; set; }

	[GeneratedSettingsProperty(DefaultValue = DeleteConfirmationPolicies.Always)]
	public partial DeleteConfirmationPolicies DeleteConfirmationPolicy { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool SelectFilesOnHover { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool DoubleClickToGoUp { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowFileExtensionWarning { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowCheckboxesWhenSelectingItems { get; set; }

	[GeneratedSettingsProperty(DefaultValue = SizeUnitTypes.BinaryUnits)]
	public partial SizeUnitTypes SizeUnitFormat { get; set; }
}
