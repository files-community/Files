using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Files.App.UserControls.Sidebar
{
	public record ItemDroppedEventArgs(object DropTarget, DataPackageView DroppedItem, SidebarItemDropPosition dropPosition, DragEventArgs RawEvent) { }
	public record ItemDragOverEventArgs(object DropTarget, DataPackageView DroppedItem, SidebarItemDropPosition dropPosition, DragEventArgs RawEvent) { }
	public record ItemContextInvokedArgs(object? Item, Point Position) { }

	public interface ISidebarViewModel
	{
		public object SidebarItems { get; }

		public void HandleItemDropped(ItemDroppedEventArgs args);

		public void HandleItemDragOver(ItemDragOverEventArgs args);

		public void HandleItemContextInvoked(object sender, ItemContextInvokedArgs args);

		public void HandleItemInvoked(object item);
	}
}
