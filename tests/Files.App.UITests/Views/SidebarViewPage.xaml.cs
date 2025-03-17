// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.UITests.Data;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace Files.App.UITests.Views
{
	public sealed partial class SidebarViewPage : Page
	{
		private ObservableCollection<TestSidebarModel> SidebarViewItems;

		public SidebarViewPage()
		{
			InitializeComponent();

			SidebarViewItems =
			[
				new TestSidebarModel { Text = "Test 1" },
				new TestSidebarModel { Text = "Test 2" },
				new TestSidebarModel { Text = "Test 3" }
			];

			Sidebar.ViewModel = new TestSidebarViewModel();
		}
	}
}
