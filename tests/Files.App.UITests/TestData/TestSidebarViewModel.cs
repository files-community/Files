using Files.App.Controls;
using Microsoft.UI.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Files.App.UITests.TestData
{
	class TestSidebarViewModel : ISidebarViewModel
	{
		public object SidebarItems { get; set; } = new ObservableCollection<TestSidebarModel>();

		public void HandleItemContextInvokedAsync(object sender, ItemContextInvokedArgs args)
		{
		}

		public Task HandleItemDragOverAsync(ItemDragOverEventArgs args)
		{
			return Task.CompletedTask;
		}

		public Task HandleItemDroppedAsync(ItemDroppedEventArgs args)
		{
			return Task.CompletedTask;
		}

		public void HandleItemInvokedAsync(object item, PointerUpdateKind pointerUpdateKind)
		{
		}
	}
}
