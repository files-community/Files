using Files.App.UserControls.Widgets;
using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace Files.App.Contexts
{
	public interface IWidgetsPageContext
	{
		/// <summary>
		/// The last right clicked item
		/// </summary>
		WidgetCardItem? RightClickedItem { get; }

		/// <summary>
		/// The last opened widget's context menu instance
		/// </summary>
		CommandBarFlyout? ItemContextFlyoutMenu { get; }

		/// <summary>
		/// An list containing all the selected tagged items
		/// </summary>
		IReadOnlyList<FileTagsItemViewModel> SelectedTaggedItems { get; }

		/// <summary>
		/// Tells whether any item has been right clicked
		/// </summary>
		bool IsAnyItemRightClicked { get; }
	}
}
