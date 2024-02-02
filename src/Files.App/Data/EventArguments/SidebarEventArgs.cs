// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Files.App.Data.EventArguments
{
	public record ItemDroppedEventArgs(object DropTarget, DataPackageView DroppedItem, SidebarItemDropPosition dropPosition, DragEventArgs RawEvent) { }

	public record ItemDragOverEventArgs(object DropTarget, DataPackageView DroppedItem, SidebarItemDropPosition dropPosition, DragEventArgs RawEvent) { }

	public record ItemContextInvokedArgs(object? Item, Point Position) { }
}
