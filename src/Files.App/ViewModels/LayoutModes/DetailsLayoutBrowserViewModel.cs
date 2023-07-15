// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.App.UserControls.Selection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using SortDirection = Files.Core.Data.Enums.SortDirection;

namespace Files.App.ViewModels.LayoutModes
{
	public class DetailsLayoutBrowserViewModel
	{
		private readonly IContentPageContext context = Ioc.Default.GetService<IContentPageContext>();

		private FolderSettingsViewModel? FolderSettings
			=> context.ShellPage?.InstanceViewModel.FolderSettings;

		public ListViewBase ListViewBase { get; set; } = null!;

		private IList<ListedItem> ListedItems
			=> ListViewBase.Items.Cast<ListedItem>().ToList();

		public ColumnsViewModel ColumnsViewModel { get; } = new();

		public IList<MenuFlyoutItemBase> ColumnHeadersMenuFlyoutItems { get; private set; }

		public ICommand ToggleColumnCommand { get; set; }

		public ICommand SetColumnsAsDefaultCommand { get; set; }

		public ICommand ResizeAllColumnsToFitCommand { get; set; }

		public ICommand UpdateSortOptionsCommand { get; }

		public DetailsLayoutBrowserViewModel()
		{
			ColumnHeadersMenuFlyoutItems = GetColumnsHeaderContextMenuFlyout();

			ToggleColumnCommand = new RelayCommand<ColumnViewModel>(ToggleColumn);
			SetColumnsAsDefaultCommand = new RelayCommand(SetColumnsAsDefault);
			ResizeAllColumnsToFitCommand = new RelayCommand(ResizeAllColumnsToFit);
			UpdateSortOptionsCommand = new RelayCommand<string>(UpdateSortOptions);
		}

		public IList<MenuFlyoutItemBase> GetColumnsHeaderContextMenuFlyout()
		{
			var contextMenuFlyoutItemModels = new List<ContextMenuFlyoutItemViewModel>()
			{
				new()
				{
					Text = "SizeAllColumnsToFit".GetLocalizedResource(),
					Command = ResizeAllColumnsToFitCommand,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
				},
				new()
				{
					Text = "Git".GetLocalizedResource(),
					ShowItem = context.ShellPage.InstanceViewModel.IsGitRepository,
					Items = new()
					{
						new()
						{
							Text = "GitStatus".GetLocalizedResource(),
							Command = ToggleColumnCommand,
							CommandParameter = ColumnsViewModel.GitStatusColumn,
							ShowItem = ColumnsViewModel.GitStatusColumn.IsHidden,
						},
						new()
						{
							Text = "DateCommitted".GetLocalizedResource(),
							Command = ToggleColumnCommand,
							CommandParameter = ColumnsViewModel.GitLastCommitDateColumn,
							ShowItem = ColumnsViewModel.GitLastCommitDateColumn.IsHidden,
						},
						new()
						{
							Text = "CommitMessage".GetLocalizedResource(),
							Command = ToggleColumnCommand,
							CommandParameter = ColumnsViewModel.GitLastCommitMessageColumn,
							ShowItem = ColumnsViewModel.GitLastCommitMessageColumn.IsHidden,
						},
						new()
						{
							Text = "Author".GetLocalizedResource(),
							Command = ToggleColumnCommand,
							CommandParameter = ColumnsViewModel.GitCommitAuthorColumn,
							ShowItem = ColumnsViewModel.GitCommitAuthorColumn.IsHidden,
						},
						new()
						{
							Text = "CommitSha".GetLocalizedResource(),
							Command = ToggleColumnCommand,
							CommandParameter = ColumnsViewModel.GitLastCommitShaColumn,
							ShowItem = ColumnsViewModel.GitLastCommitShaColumn.IsHidden,
						},
					}
				},
				new()
				{
					Text = "Tag".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.TagColumn,
					ShowItem = ColumnsViewModel.TagColumn.IsHidden,
				},
				new()
				{
					Text = "PathColumn".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.PathColumn,
					ShowItem = ColumnsViewModel.PathColumn.IsHidden,
				},
				new()
				{
					Text = "DetailsViewHeaderFlyout_ShowOriginalPath/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.OriginalPathColumn,
					ShowItem = ColumnsViewModel.OriginalPathColumn.IsHidden,
				},
				new()
				{
					Text = "DetailsViewHeaderFlyout_ShowDateDeleted/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.DateDeletedColumn,
					ShowItem = ColumnsViewModel.DateDeletedColumn.IsHidden,
				},
				new()
				{
					Text = "DetailsViewHeaderFlyout_ShowDateModified/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.DateModifiedColumn,
					ShowItem = ColumnsViewModel.DateModifiedColumn.IsHidden,
				},
				new()
				{
					Text = "DetailsViewHeaderFlyout_ShowDateCreated/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.DateCreatedColumn,
					ShowItem = ColumnsViewModel.DateCreatedColumn.IsHidden,
				},
				new()
				{
					Text = "DetailsViewHeaderFlyout_ShowItemType/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.ItemTypeColumn,
					ShowItem = ColumnsViewModel.ItemTypeColumn.IsHidden,
				},
				new()
				{
					Text = "DetailsViewHeaderFlyout_ShowItemSize/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.SizeColumn,
					ShowItem = ColumnsViewModel.SizeColumn.IsHidden,
				},
				new()
				{
					Text = "DetailsViewHeaderFlyout_ShowSyncStatus/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.StatusColumn,
					ShowItem = ColumnsViewModel.StatusColumn.IsHidden,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
				},
				new()
				{
					Text = "SetAsDefault".GetLocalizedResource(),
					Command = SetColumnsAsDefaultCommand,
				},
			};

			var list = Helpers.ContextFlyouts.ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(contextMenuFlyoutItemModels);

			return list;
		}

		private void ToggleColumn(ColumnViewModel? item)
		{
			// Toggle the column
			item.UserCollapsed = !item.UserCollapsed;

			// Update settings
			FolderSettings.ColumnsViewModel = ColumnsViewModel;
		}

		private void SetColumnsAsDefault()
		{
			FolderSettings.SetDefaultLayoutPreferences(ColumnsViewModel);
		}

		private void ResizeAllColumnsToFit()
		{
			// If there aren't items, do not make columns fit
			if (!ListedItems.Any())
				return;

			// For scalability, just count the # of public `ColumnViewModel` properties in ColumnsViewModel
			int totalColumnCount =
				ColumnsViewModel
					.GetType()
					.GetProperties()
					.Count(prop => prop.PropertyType == typeof(ColumnViewModel));

			for (int columnIndex = 1; columnIndex <= totalColumnCount; columnIndex++)
				ResizeColumnToFit(columnIndex);
		}

		private void UpdateSortOptions(string? option)
		{
			if (!Enum.TryParse<SortOption>(option, out var val))
				return;

			if (FolderSettings.DirectorySortOption == val)
			{
				FolderSettings.DirectorySortDirection = (SortDirection)(((int)FolderSettings.DirectorySortDirection + 1) % 2);
			}
			else
			{
				FolderSettings.DirectorySortOption = val;
				FolderSettings.DirectorySortDirection = SortDirection.Ascending;
			}
		}

		public void ResizeColumnToFit(int columnToResize)
		{
			if (!ListedItems.Any())
				return;

			// Get max item length that is requested to resize to fit
			var maxItemLength = columnToResize switch
			{
				// Item icon
				1 => 40,
				// Item name
				2 => ListedItems.Select(x => x.Name?.Length ?? 0).Max(),
				4 => ListedItems.Select(x => (x as GitItem)?.GitLastCommitDateHumanized?.Length ?? 0).Max(), // git
				5 => ListedItems.Select(x => (x as GitItem)?.GitLastCommitMessage?.Length ?? 0).Max(), // git
				6 => ListedItems.Select(x => (x as GitItem)?.GitLastCommitAuthor?.Length ?? 0).Max(), // git
				7 => ListedItems.Select(x => (x as GitItem)?.GitLastCommitSha?.Length ?? 0).Max(), // git
				8 => ListedItems.Select(x => x.FileTagsUI?.Sum(x => x?.Name?.Length ?? 0) ?? 0).Max(), // file tag column
				9 => ListedItems.Select(x => x.ItemPath?.Length ?? 0).Max(), // path column
				10 => ListedItems.Select(x => (x as RecycleBinItem)?.ItemOriginalPath?.Length ?? 0).Max(), // original path column
				11 => ListedItems.Select(x => (x as RecycleBinItem)?.ItemDateDeleted?.Length ?? 0).Max(), // date deleted column
				12 => ListedItems.Select(x => x.ItemDateModified?.Length ?? 0).Max(), // date modified column
				13 => ListedItems.Select(x => x.ItemDateCreated?.Length ?? 0).Max(), // date created column
				14 => ListedItems.Select(x => x.ItemType?.Length ?? 0).Max(), // item type column
				15 => ListedItems.Select(x => x.FileSize?.Length ?? 0).Max(), // item size column
				_ => 20 // cloud status column
			};

			// If called programmatically, the column could be hidden
			// In this case, resizing doesn't need to be done at all
			if (maxItemLength == 0)
				return;

			// Estimate columns size to fit judging from max length item
			var columnSizeToFit = MeasureColumnEstimate(columnToResize, 5, maxItemLength);

			if (columnSizeToFit > 1)
			{
				var column = columnToResize switch
				{
					2 => ColumnsViewModel.NameColumn,
					3 => ColumnsViewModel.GitStatusColumn,
					4 => ColumnsViewModel.GitLastCommitDateColumn,
					5 => ColumnsViewModel.GitLastCommitMessageColumn,
					6 => ColumnsViewModel.GitCommitAuthorColumn,
					7 => ColumnsViewModel.GitLastCommitShaColumn,
					8 => ColumnsViewModel.TagColumn,
					9 => ColumnsViewModel.PathColumn,
					10 => ColumnsViewModel.OriginalPathColumn,
					11 => ColumnsViewModel.DateDeletedColumn,
					12 => ColumnsViewModel.DateModifiedColumn,
					13 => ColumnsViewModel.DateCreatedColumn,
					14 => ColumnsViewModel.ItemTypeColumn,
					15 => ColumnsViewModel.SizeColumn,
					_ => ColumnsViewModel.StatusColumn
				};

				// Overestimate
				if (columnToResize == 2) // file name column
					columnSizeToFit += 20;

				var minFitLength = Math.Max(columnSizeToFit, column.NormalMinLength);
				var maxFitLength = Math.Min(minFitLength + 36, column.NormalMaxLength); // 36 to account for SortIcon & padding

				// Set size
				column.UserLength = new GridLength(maxFitLength, GridUnitType.Pixel);
			}

			FolderSettings.ColumnsViewModel = ColumnsViewModel;
		}

		private double MeasureColumnEstimate(int columnIndex, int measureItemsCount, int maxItemLength)
		{
			if (columnIndex == 15) // sync status
				return maxItemLength;

			if (columnIndex == 8) // file tag
				return MeasureTagColumnEstimate(columnIndex);

			return MeasureTextColumnEstimate(columnIndex, measureItemsCount, maxItemLength);
		}

		private double MeasureTagColumnEstimate(int columnIndex)
		{
			var grids = DependencyObjectHelpers
				.FindChildren<Grid>(ListViewBase.ItemsPanelRoot)
				.Where(grid => IsCorrectColumn(grid, columnIndex));

			// Get the list of stack panels with the most letters
			var stackPanels = grids
				.Select(DependencyObjectHelpers.FindChildren<StackPanel>)
				.OrderByDescending(sps => sps.Select(sp => DependencyObjectHelpers.FindChildren<TextBlock>(sp).Select(tb => tb.Text.Length).Sum()).Sum())
				.First()
				.ToArray();

			var mesuredSize = stackPanels.Select(x =>
			{
				x.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

				return x.DesiredSize.Width;
			}).Sum();

			if (stackPanels.Length >= 2)
				mesuredSize += 4 * (stackPanels.Length - 1); // The spacing between the tags

			return mesuredSize;
		}

		private double MeasureTextColumnEstimate(int columnIndex, int measureItemsCount, int maxItemLength)
		{
			var tbs = DependencyObjectHelpers
				.FindChildren<TextBlock>(ListViewBase.ItemsPanelRoot)
				.Where(tb => IsCorrectColumn(tb, columnIndex));

			// heuristic: usually, text with more letters are wider than shorter text with wider letters
			// with this, we can calculate avg width using longest text(s) to avoid overshooting the width
			var widthPerLetter = tbs
				.OrderByDescending(x => x.Text.Length)
				.Where(tb => !string.IsNullOrEmpty(tb.Text))
				.Take(measureItemsCount)
				.Select(tb =>
				{
					var sampleTb = new TextBlock { Text = tb.Text, FontSize = tb.FontSize, FontFamily = tb.FontFamily };
					sampleTb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

					return sampleTb.DesiredSize.Width / Math.Max(1, tb.Text.Length);
				});

			if (!widthPerLetter.Any())
				return 0;

			// Take weighted avg between mean and max since width is an estimate
			var weightedAvg = (widthPerLetter.Average() + widthPerLetter.Max()) / 2;
			return weightedAvg * maxItemLength;
		}

		private bool IsCorrectColumn(FrameworkElement element, int columnIndex)
		{
			int columnIndexFromName = element.Name switch
			{
				"ItemName" => 2,
				"ItemGitStatusTextBlock" => 3,
				"ItemGitLastCommitDateTextBlock" => 4,
				"ItemGitLastCommitMessageTextBlock" => 5,
				"ItemGitCommitAuthorTextBlock" => 6,
				"ItemGitLastCommitShaTextBlock" => 7,
				"ItemTagGrid" => 8,
				"ItemPath" => 9,
				"ItemOriginalPath" => 10,
				"ItemDateDeleted" => 11,
				"ItemDateModified" => 12,
				"ItemDateCreated" => 13,
				"ItemType" => 14,
				"ItemSize" => 15,
				"ItemStatus" => 16,
				_ => -1,
			};

			return columnIndexFromName != -1 && columnIndexFromName == columnIndex;
		}
	}
}
