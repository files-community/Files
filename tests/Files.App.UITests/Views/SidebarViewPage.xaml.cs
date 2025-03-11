using Files.App.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.UITests.Views
{
	class TestSidebarModel : ISidebarItemModel
	{
		public object? Children => null;

		public IconSource? IconSource { get; set; }

		public bool IsExpanded { get; set; }

		public required string Text { get; set; }

		public object ToolTip => "";

		public bool PaddedItem => false;

		public event PropertyChangedEventHandler? PropertyChanged;
	}

	class TestViewModel : ISidebarViewModel
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

	public sealed partial class SidebarViewPage : Page
	{
		private ObservableCollection<TestSidebarModel> sidebarModels = new();

		public SidebarViewPage()
		{
			this.InitializeComponent();

			sidebarModels.Add(new TestSidebarModel { Text = "Test 1" });
			sidebarModels.Add(new TestSidebarModel { Text = "Test 2" });
			sidebarModels.Add(new TestSidebarModel { Text = "Test 3" });

			Sidebar.ViewModel = new TestViewModel();
		}
	}
}
