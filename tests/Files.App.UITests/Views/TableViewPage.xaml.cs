// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Controls;
using Files.App.UITests.Data;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.UITests.Views
{
	internal sealed partial class TableViewPage : Page
	{
		private const int GeneratedItemCount = 5000;

		private static readonly TableViewItemModel[] SampleItems =
		[
			new() { Name = "Designs", DateUpdated = new DateTimeOffset(2026, 3, 8, 9, 14, 0, TimeSpan.FromHours(9)), Type = "File folder", Size = string.Empty },
			new() { Name = "Quarterly Report Q1 2026.xlsx", DateUpdated = new DateTimeOffset(2026, 3, 8, 8, 2, 0, TimeSpan.FromHours(9)), Type = "Microsoft Excel Worksheet", Size = "2.4 MB" },
			new() { Name = "Product Roadmap.pptx", DateUpdated = new DateTimeOffset(2026, 3, 7, 16, 42, 0, TimeSpan.FromHours(9)), Type = "Microsoft PowerPoint Presentation", Size = "18.7 MB" },
			new() { Name = "Brand Guidelines.pdf", DateUpdated = new DateTimeOffset(2026, 3, 7, 14, 18, 0, TimeSpan.FromHours(9)), Type = "PDF Document", Size = "6.1 MB" },
			new() { Name = "HeroBanner_Final.png", DateUpdated = new DateTimeOffset(2026, 3, 6, 11, 31, 0, TimeSpan.FromHours(9)), Type = "PNG File", Size = "4.8 MB" },
			new() { Name = "SpringCampaign", DateUpdated = new DateTimeOffset(2026, 3, 6, 9, 7, 0, TimeSpan.FromHours(9)), Type = "File folder", Size = string.Empty },
			new() { Name = "Invoice_10482.docx", DateUpdated = new DateTimeOffset(2026, 3, 5, 15, 26, 0, TimeSpan.FromHours(9)), Type = "Microsoft Word Document", Size = "184 KB" },
			new() { Name = "Onboarding Checklist.txt", DateUpdated = new DateTimeOffset(2026, 3, 5, 10, 13, 0, TimeSpan.FromHours(9)), Type = "Text Document", Size = "12 KB" },
			new() { Name = "Team Photo.jpg", DateUpdated = new DateTimeOffset(2026, 3, 4, 18, 54, 0, TimeSpan.FromHours(9)), Type = "JPEG File", Size = "3.2 MB" },
			new() { Name = "ReleaseNotes-v3.8.md", DateUpdated = new DateTimeOffset(2026, 3, 4, 13, 22, 0, TimeSpan.FromHours(9)), Type = "Markdown Source File", Size = "28 KB" },
			new() { Name = "Customer Interviews", DateUpdated = new DateTimeOffset(2026, 3, 3, 17, 40, 0, TimeSpan.FromHours(9)), Type = "File folder", Size = string.Empty },
			new() { Name = "Demo Reel.mp4", DateUpdated = new DateTimeOffset(2026, 3, 3, 11, 8, 0, TimeSpan.FromHours(9)), Type = "MP4 Video", Size = "124 MB" },
			new() { Name = "appsettings.json", DateUpdated = new DateTimeOffset(2026, 3, 2, 20, 49, 0, TimeSpan.FromHours(9)), Type = "JSON Source File", Size = "5 KB" },
			new() { Name = "prototype-v12.fig", DateUpdated = new DateTimeOffset(2026, 3, 2, 14, 16, 0, TimeSpan.FromHours(9)), Type = "FIG File", Size = "31.5 MB" },
			new() { Name = "Assets", DateUpdated = new DateTimeOffset(2026, 3, 1, 16, 3, 0, TimeSpan.FromHours(9)), Type = "File folder", Size = string.Empty },
			new() { Name = "archive-2025.zip", DateUpdated = new DateTimeOffset(2026, 2, 28, 19, 12, 0, TimeSpan.FromHours(9)), Type = "Compressed (zipped) Folder", Size = "842 MB" },
			new() { Name = "setup.exe", DateUpdated = new DateTimeOffset(2026, 2, 28, 10, 55, 0, TimeSpan.FromHours(9)), Type = "Application", Size = "67.9 MB" },
			new() { Name = "Vacation Budget.csv", DateUpdated = new DateTimeOffset(2026, 2, 27, 13, 47, 0, TimeSpan.FromHours(9)), Type = "CSV File", Size = "96 KB" },
			new() { Name = "wireframes.sketch", DateUpdated = new DateTimeOffset(2026, 2, 26, 9, 33, 0, TimeSpan.FromHours(9)), Type = "SKETCH File", Size = "14.2 MB" },
			new() { Name = "Meeting Recording.m4a", DateUpdated = new DateTimeOffset(2026, 2, 25, 17, 11, 0, TimeSpan.FromHours(9)), Type = "M4A Audio File", Size = "48.6 MB" },
		];

		public ObservableCollection<TableViewColumnModel> Columns { get; } =
		[
			new("Name" , nameof(TableViewItemModel.Name)),
			new("Date updated", nameof(TableViewItemModel.DateUpdated), TableViewColumnValueType.DateTimeOffset),
			new("Type" , nameof(TableViewItemModel.Type)),
			new("Size" , nameof(TableViewItemModel.Size)),
		];

		public BulkConcurrentObservableCollection<TableViewItemModel> Items { get; }

		public TableViewPage()
		{
			Items = [];

			InitializeComponent();
		}

		private async void TableViewPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			var list = await Task.Run(() => GenerateItems(GeneratedItemCount));

			await DispatcherQueue.EnqueueAsync(() =>
			{
				Items.Clear();
				Items.AddRange(list);
			});
		}

		private void ToggleSourceTable_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			SourceTableHost.Content = SourceTableHost.Content is null ? SourceTableView : null;
		}

		private void UpdateFirstItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			if (Items.Count > 0)
				Items[0].Name = $"Updated at {DateTimeOffset.Now:T}";
		}

		private void AddOrRemoveColumn_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			if (Columns.Count > 4)
				Columns.RemoveAt(Columns.Count - 1);
			else
				Columns.Add(new("Name copy", nameof(TableViewItemModel.Name)));
		}

		private void TableView_Sorting(object? sender, TableViewColumnSortingEventArgs e)
		{
			IEnumerable<TableViewItemModel> sortedItems = (e.Column.Binding, e.SortDirection) switch
			{
				(nameof(TableViewItemModel.Name), ListSortDirection.Ascending) => Items.OrderBy(item => item.Name),
				(nameof(TableViewItemModel.Name), _) => Items.OrderByDescending(item => item.Name),
				(nameof(TableViewItemModel.DateUpdated), ListSortDirection.Ascending) => Items.OrderBy(item => item.DateUpdated),
				(nameof(TableViewItemModel.DateUpdated), _) => Items.OrderByDescending(item => item.DateUpdated),
				(nameof(TableViewItemModel.Type), ListSortDirection.Ascending) => Items.OrderBy(item => item.Type),
				(nameof(TableViewItemModel.Type), _) => Items.OrderByDescending(item => item.Type),
				(nameof(TableViewItemModel.Size), ListSortDirection.Ascending) => Items.OrderBy(item => item.Size),
				_ => Items.OrderByDescending(item => item.Size),
			};

			var materializedItems = sortedItems.ToList();
			Items.Clear();
			Items.AddRange(materializedItems);
			SortStatusTextBlock.Text = $"{e.Column.Header}: {e.SortDirection}";
		}

		private static List<TableViewItemModel> GenerateItems(int count)
		{
			var items = new List<TableViewItemModel>(count);
			var sampleCount = SampleItems.Length;
			for (var i = 0; i < count; i++)
			{
				var template = SampleItems[i % sampleCount];
				items.Add(new TableViewItemModel
				{
					Name = $"{template.Name} ({i + 1})",
					DateUpdated = template.DateUpdated?.AddMinutes(-i),
					Type = template.Type,
					Size = template.Size
				});
			}

			return items;
		}
	}
}
