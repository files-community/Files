// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets the layout templates, or null for the built-in templates.
		/// </summary>
		List<LayoutTemplate>? LayoutTemplates { get; set; }

		/// <summary>
		/// Gets or sets the conditions that make folders use a layout template, or null for the built-in conditions.
		/// </summary>
		List<LayoutRule>? LayoutRules { get; set; }

		/// <summary>
		/// Removes the specified settings, which are no longer used, and returns the ones that were saved.
		/// </summary>
		IReadOnlyDictionary<string, JsonElement> TakeLegacySettings(IEnumerable<string> names);

		/// <summary>
		/// Gets or sets a value indicating whether or not columns in the Details layout should
		/// automatically resize to fit content on navigation, item add/remove, and rename.
		/// </summary>
		bool AutoSizeColumnsInDetailsLayout { get; set; }

		/// <summary>
		/// Item size in the Details View
		/// </summary>
		DetailsViewSizeKind DetailsViewSize { get; set; }

		/// <summary>
		/// Item size in the List View
		/// </summary>
		ListViewSizeKind ListViewSize { get; set; }

		/// <summary>
		/// Item size in the Cards View
		/// </summary>
		CardsViewSizeKind CardsViewSize { get; set; }

		/// <summary>
		/// Item size in the Grid View
		/// </summary>
		GridViewSizeKind GridViewSize { get; set; }

		/// <summary>
		/// Item size in the Columns View
		/// </summary>
		ColumnsViewSizeKind ColumnsViewSize { get; set; }
	}
}
