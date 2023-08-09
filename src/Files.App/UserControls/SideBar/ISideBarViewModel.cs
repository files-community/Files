// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

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
		object SidebarItems { get; }

		void HandleItemContextInvoked(object sender, ItemContextInvokedArgs args);

		void HandleItemDragOver(ItemDragOverEventArgs args);

		void HandleItemDropped(ItemDroppedEventArgs args);

		void HandleItemInvoked(object item);
	}
}
