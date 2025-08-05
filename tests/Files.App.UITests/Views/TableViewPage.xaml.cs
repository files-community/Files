// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.UITests.Data;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Files.App.UITests.Views
{
	internal sealed partial class TableViewPage : Page
	{
		public ObservableCollection<TableViewItemModel> Items { get; set; }

		public TableViewPage()
		{
			Items = [];

			InitializeComponent();
		}

		private async void TableViewPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			await DispatcherQueue.EnqueueAsync(() =>
			{
				for (int index = 0; index < 4000; index++)
				{
					Items.Add(new() { Name = $"Name {index}", DateUpdated = $"DateUpdated {index}", Size = $"Size {index}", Type = $"Type {index}" });
				}
			});
		}
	}
}
