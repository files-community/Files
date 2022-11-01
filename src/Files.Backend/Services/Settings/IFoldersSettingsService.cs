using Files.Shared.Enums;
using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
	public interface IFoldersSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets a value indicating whether or not the date column should be visible by default.
		/// </summary>
		bool ShowDateColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the date created column should be visible by default.
		/// </summary>
		bool ShowDateCreatedColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the type column should be visible by default.
		/// </summary>
		bool ShowTypeColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the size column should be visible by default.
		/// </summary>
		bool ShowSizeColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the filetag column should be visible by default.
		/// </summary>
		bool ShowFileTagColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default layout mode.
		/// </summary>
		FolderLayoutModes DefaultLayoutMode { get; set; }

		/// <summary>
		/// Gets or sets a value indicating tags column's default width
		/// </summary>
		double TagColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating name column's default width
		/// </summary>
		double NameColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating date modified column's default width
		/// </summary>
		double DateModifiedColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating item type column's default width
		/// </summary>
		double TypeColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating date created column's default width
		/// </summary>
		double DateCreatedColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating size column's default width
		/// </summary>
		double SizeColumnWidth { get; set; }

		/// <summary>
		/// Enable overriding folder preferencess in individual directories
		/// </summary>
		bool EnableOverridingFolderPreferences { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not hidden items should be visible.
		/// </summary>
		bool ShowHiddenItems { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not protected system files should be visible.
		/// </summary>
		bool ShowProtectedSystemFiles { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not alternate data streams should be visible.
		/// </summary>
		bool AreAlternateStreamsVisible { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to display dot files.
		/// </summary>
		bool ShowDotFiles { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not items should open with one click.
		/// </summary>
		bool OpenItemsWithOneClick { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not folders should open with two clicks in ColumnsLayout.
		/// </summary>
		bool ColumnLayoutOpenFoldersWithOneClick { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to open folders in new tab.
		/// </summary>
		bool OpenFoldersInNewTab { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to show folder size.
		/// </summary>
		bool CalculateFolderSizes { get; set; }
	}
}
