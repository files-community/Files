using Files.App.UITests.TestData;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace Files.App.UITests.Views
{
	public sealed partial class SidebarViewPage : Page
	{
		private ObservableCollection<TestSidebarModel> sidebarModels = new();

		public SidebarViewPage()
		{
			InitializeComponent();

			sidebarModels.Add(new TestSidebarModel { Text = "Test 1" });
			sidebarModels.Add(new TestSidebarModel { Text = "Test 2" });
			sidebarModels.Add(new TestSidebarModel { Text = "Test 3" });

			Sidebar.ViewModel = new TestSidebarViewModel();
		}
	}
}
