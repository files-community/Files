// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Controls;
using Files.App.UITests.Data;
using Microsoft.UI.Xaml;
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
		private const string FolderGlyph = "\uE8B7";
		private const string FileGlyph = "\uE8A5";

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

		public ObservableCollection<TableViewItemModel> Items { get; }

		public ObservableCollection<DetailsViewItemModel> DetailsItems { get; } =
		[
			new() { Name = "Assets", DateModified = DetailsDate(15, 9, 12), Type = "File folder", Size = "", IconGlyph = FolderGlyph },
			new() { Name = "Designs", DateModified = DetailsDate(14, 17, 45), Type = "File folder", Size = "", IconGlyph = FolderGlyph },
			new() { Name = "Quarterly Report Q2 2026.xlsx", DateModified = DetailsDate(14, 15, 23), Type = "Microsoft Excel Worksheet", Size = "2.4 MB", IconGlyph = FileGlyph },
			new() { Name = "Product Roadmap.pptx", DateModified = DetailsDate(13, 18, 2), Type = "Microsoft PowerPoint Presentation", Size = "18.7 MB", IconGlyph = FileGlyph },
			new() { Name = "Brand Guidelines.pdf", DateModified = DetailsDate(13, 14, 18), Type = "PDF Document", Size = "6.1 MB", IconGlyph = FileGlyph },
			new() { Name = "HeroBanner_Final.png", DateModified = DetailsDate(12, 11, 31), Type = "PNG File", Size = "4.8 MB", IconGlyph = FileGlyph },
			new() { Name = "Invoice_10482.docx", DateModified = DetailsDate(11, 15, 26), Type = "Microsoft Word Document", Size = "184 KB", IconGlyph = FileGlyph },
			new() { Name = "Onboarding Checklist.txt", DateModified = DetailsDate(11, 10, 13), Type = "Text Document", Size = "12 KB", IconGlyph = FileGlyph },
			new() { Name = "Team Photo.jpg", DateModified = DetailsDate(10, 18, 54), Type = "JPEG File", Size = "3.2 MB", IconGlyph = FileGlyph },
			new() { Name = "ReleaseNotes-v4.0.md", DateModified = DetailsDate(10, 13, 22), Type = "Markdown Source File", Size = "28 KB", IconGlyph = FileGlyph },
			new() { Name = "Demo Reel.mp4", DateModified = DetailsDate(9, 11, 8), Type = "MP4 Video", Size = "124 MB", IconGlyph = FileGlyph },
			new() { Name = "appsettings.json", DateModified = DetailsDate(8, 20, 49), Type = "JSON Source File", Size = "5 KB", IconGlyph = FileGlyph },
		];

		public ObservableCollection<ToolbarCustomizationItemModel> ToolbarItems { get; } = [];

		public TableViewPage()
		{
			Items = [];

			InitializeComponent();

			ResetToolbarItems();
		}

		private async void TableViewPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			var list = await Task.Run(() => GenerateItems(GeneratedItemCount));

			await DispatcherQueue.EnqueueAsync(() =>
			{
				Items.Clear();
				foreach (var item in list)
					Items.Add(item);
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
			foreach (var item in materializedItems)
				Items.Add(item);
			SortStatusTextBlock.Text = $"{e.Column.Header}: {e.SortDirection}";
		}

		private void DetailsTable_Sorting(object? sender, TableViewColumnSortingEventArgs e)
		{
			IEnumerable<DetailsViewItemModel> sortedItems = (e.Column.Binding, e.SortDirection) switch
			{
				(nameof(DetailsViewItemModel.Name), ListSortDirection.Ascending) => DetailsItems.OrderBy(item => item.Name),
				(nameof(DetailsViewItemModel.Name), _) => DetailsItems.OrderByDescending(item => item.Name),
				(nameof(DetailsViewItemModel.DateModified), ListSortDirection.Ascending) => DetailsItems.OrderBy(item => item.DateModified),
				(nameof(DetailsViewItemModel.DateModified), _) => DetailsItems.OrderByDescending(item => item.DateModified),
				(nameof(DetailsViewItemModel.Type), ListSortDirection.Ascending) => DetailsItems.OrderBy(item => item.Type),
				(nameof(DetailsViewItemModel.Type), _) => DetailsItems.OrderByDescending(item => item.Type),
				(nameof(DetailsViewItemModel.Size), ListSortDirection.Ascending) => DetailsItems.OrderBy(item => item.Size),
				_ => DetailsItems.OrderByDescending(item => item.Size),
			};

			var materializedItems = sortedItems.ToList();
			DetailsItems.Clear();
			foreach (var item in materializedItems)
				DetailsItems.Add(item);

		}

		private void RemoveToolbarItem_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not FrameworkElement { DataContext: ToolbarCustomizationItemModel item })
				return;

			ToolbarItems.Remove(item);
		}

		private void ResetToolbarItems()
		{
			ToolbarItems.Clear();
			AddToolbarItem(CreateToolbarItem("New", "\uE710", showLabel: true));
			AddToolbarItem(CreateToolbarItem("Cut", "\uE8C6"));
			AddToolbarItem(CreateToolbarItem("Copy", "\uE8C8"));
			AddToolbarItem(CreateToolbarItem("Paste", "\uE77F"));
			AddToolbarItem(CreateToolbarSeparator());
			AddToolbarItem(CreateToolbarItem("Rename", "\uE8AC", showLabel: true));
		}

		private void AddToolbarItem(ToolbarCustomizationItemModel item)
		{
			ToolbarItems.Add(item);
		}

		private static ToolbarCustomizationItemModel CreateToolbarSeparator()
		{
			return new()
			{
				DisplayName = "Separator",
				IconGlyph = FileGlyph,
				HasIcon = false,
				IsSeparator = true,
				ShowIcon = false,
				ShowLabel = false,
			};
		}

		private static ToolbarCustomizationItemModel CreateToolbarItem(string displayName, string iconGlyph, bool showLabel = false, bool hasIcon = true)
		{
			return new()
			{
				DisplayName = displayName,
				IconGlyph = iconGlyph,
				HasIcon = hasIcon,
				ShowIcon = hasIcon,
				ShowLabel = showLabel,
			};
		}

		private static DateTimeOffset DetailsDate(int day, int hour, int minute)
		{
			return new(2026, 7, day, hour, minute, 0, TimeSpan.FromHours(9));
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
