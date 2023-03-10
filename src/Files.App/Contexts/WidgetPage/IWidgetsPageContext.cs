using Files.App.UserControls.Widgets;
using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace Files.App.Contexts
{
	public interface IWidgetsPageContext
	{
		WidgetCardItem? RightClickedItem { get; }

		CommandBarFlyout? ItemContextFlyoutMenu { get; }

		IEnumerable<FileTagsItemViewModel> SelectedTaggedItems { get; set; }

		bool IsAnyItemRightClicked { get; }
	}
}
