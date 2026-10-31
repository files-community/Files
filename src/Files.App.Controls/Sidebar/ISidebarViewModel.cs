// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Files.App.Controls
{
	public record ItemInvokedEventArgs(PointerUpdateKind PointerUpdateKind) { }
	public record ItemDroppedEventArgs(object DropTarget, DataPackageView DroppedItem, SidebarItemDropPosition dropPosition, DragEventArgs RawEvent) { }
	public record ItemDragOverEventArgs(object DropTarget, DataPackageView DroppedItem, SidebarItemDropPosition dropPosition, DragEventArgs RawEvent)
	{
		private readonly List<Task> deferrals = [];

		/// <summary>
		/// Keeps the drag-over operation open until <paramref name="task"/> completes.
		/// </summary>
		public void DeferUntil(Task task) => deferrals.Add(task);

		internal Task WhenDeferralsCompleted() => Task.WhenAll(deferrals);
	}
	public record ItemContextInvokedArgs(object? Item, Point Position) { }
}
