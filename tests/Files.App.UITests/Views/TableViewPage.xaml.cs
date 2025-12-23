// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.UITests.Data;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
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
			var list = await Task.Run(() =>
			{
				var tmp = new List<TableViewItemModel>(/*4000*/20);
				for (int index = 0; index < /*4000*/20; index++)
				{
					tmp.Add(new() { Name = $"Name {index}", DateUpdated = $"DateUpdated {index}", Size = $"Size {index}", Type = $"Type {index}" });
				}
				return tmp;
			});

			await DispatcherQueue.EnqueueAsync(() =>
			{
				Items.Clear();

				// TODO:
				//   This invokes 4000 collection changed events and thus cause a hang,
				//   we should replace this with AddRange (which does not exist out of box) in the future
				foreach (var item in list)
					Items.Add(item);

				Items = new(list);
			});

		}
	}
}
