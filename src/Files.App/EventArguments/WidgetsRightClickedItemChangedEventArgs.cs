using Files.App.UserControls.Widgets;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.EventArguments
{
	public record WidgetsRightClickedItemChangedEventArgs(WidgetCardItem Item, CommandBarFlyout Flyout);
}
