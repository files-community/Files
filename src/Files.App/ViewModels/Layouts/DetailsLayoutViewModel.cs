using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.ViewModels.Layouts
{
	public class DetailsLayoutViewModel : ObservableObject
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public ObservableCollection<DetailsLayoutColumnItem> ColumnItems { get; }

		private LayoutPreferencesManager LayoutPreferencesManager
			=> ContentPageContext.ShellPage!.InstanceViewModel.FolderSettings;

		private ItemViewModel FilesystemViewModel
			=> ContentPageContext.ShellPage!.FilesystemViewModel;

		public ICommand UpdateSortOptionsCommand { get; }
		public ICommand ToggleColumnVisibilityCommand { get; }

		public DetailsLayoutViewModel()
		{
			ColumnItems = new();

			UpdateSortOptionsCommand = new RelayCommand<string>(ExecuteUpdateSortOptionsCommand);
			ToggleColumnVisibilityCommand = new RelayCommand(ExecuteToggleColumnVisibilityCommand);
		}

		public void InitializeColumns()
		{
			foreach (var item in LayoutPreferencesManager.ColumnItems)
				ColumnItems.Add((DetailsLayoutColumnItem)item);

			FilesystemViewModel.EnabledGitProperties = GetEnabledGitColumns();
		}

		public void UpdateDetailsLayoutColumnsVisibilities(PageTypeUpdatedEventArgs e)
		{
			// When page changed

			if (e.IsTypeRecycleBin)
			{
				GetColumnItem(DetailsLayoutColumnKind.OriginalPath)?.Show();
				GetColumnItem(DetailsLayoutColumnKind.DateDeleted)?.Show();
			}
			else
			{
				GetColumnItem(DetailsLayoutColumnKind.OriginalPath)?.Hide();
				GetColumnItem(DetailsLayoutColumnKind.DateDeleted)?.Hide();
			}

			if (e.IsTypeCloudDrive)
				GetColumnItem(DetailsLayoutColumnKind.CloudSyncStatus)?.Show();
			else
				GetColumnItem(DetailsLayoutColumnKind.CloudSyncStatus)?.Hide();

			if (e.IsTypeGitRepository && !e.IsTypeSearchResults)
			{
				GetColumnItem(DetailsLayoutColumnKind.GitStatus)?.Show();
				GetColumnItem(DetailsLayoutColumnKind.GitCommitAuthor)?.Show();
				GetColumnItem(DetailsLayoutColumnKind.GitLastCommitDate)?.Show();
				GetColumnItem(DetailsLayoutColumnKind.GitLastCommitMessage)?.Show();
				GetColumnItem(DetailsLayoutColumnKind.GitLastCommitSha)?.Show();
			}
			else
			{
				GetColumnItem(DetailsLayoutColumnKind.GitStatus)?.Hide();
				GetColumnItem(DetailsLayoutColumnKind.GitCommitAuthor)?.Hide();
				GetColumnItem(DetailsLayoutColumnKind.GitLastCommitDate)?.Hide();
				GetColumnItem(DetailsLayoutColumnKind.GitLastCommitMessage)?.Hide();
				GetColumnItem(DetailsLayoutColumnKind.GitLastCommitSha)?.Hide();
			}

			if (e.IsTypeSearchResults)
				GetColumnItem(DetailsLayoutColumnKind.Path)?.Show();
			else
				GetColumnItem(DetailsLayoutColumnKind.Path)?.Hide();

			UpdateSortIndicator();
		}

		public void UpdateSortIndicator()
		{
			// Called when header clicked

			_ = LayoutPreferencesManager.DirectorySortOption switch
			{
				SortOption.Name => SetSortDirection(DetailsLayoutColumnKind.Name),
				SortOption.FileTag => SetSortDirection(DetailsLayoutColumnKind.Tags),
				SortOption.Path => SetSortDirection(DetailsLayoutColumnKind.Path),
				SortOption.OriginalFolder => SetSortDirection(DetailsLayoutColumnKind.OriginalPath),
				SortOption.DateDeleted => SetSortDirection(DetailsLayoutColumnKind.DateDeleted),
				SortOption.DateModified => SetSortDirection(DetailsLayoutColumnKind.DateModified),
				SortOption.DateCreated => SetSortDirection(DetailsLayoutColumnKind.DateCreated),
				SortOption.FileType => SetSortDirection(DetailsLayoutColumnKind.Type),
				SortOption.Size => SetSortDirection(DetailsLayoutColumnKind.Size),
				SortOption.SyncStatus => SetSortDirection(DetailsLayoutColumnKind.CloudSyncStatus),
				_ => SetSortDirection(null),
			};

			bool SetSortDirection(DetailsLayoutColumnKind? kind)
			{
				if (kind is null)
					return false;

				var item = GetColumnItem(kind);
				if (item is null)
					return false;

				item.SortDirection = LayoutPreferencesManager.DirectorySortDirection;

				return true;
			}
		}

		public void UpdateColumnLayout()
		{
			// Called when splitter moved

			ColumnsViewModel.IconColumn.UserLengthPixels = Column2.ActualWidth;
			ColumnsViewModel.NameColumn.UserLengthPixels = Column3.ActualWidth;
			ColumnsViewModel.GitStatusColumn.UserLengthPixels = GitStatusColumnDefinition.ActualWidth;
			ColumnsViewModel.GitLastCommitDateColumn.UserLengthPixels = GitLastCommitDateColumnDefinition.ActualWidth;
			ColumnsViewModel.GitLastCommitMessageColumn.UserLengthPixels = GitLastCommitMessageColumnDefinition.ActualWidth;
			ColumnsViewModel.GitCommitAuthorColumn.UserLengthPixels = GitCommitAuthorColumnDefinition.ActualWidth;
			ColumnsViewModel.GitLastCommitShaColumn.UserLengthPixels = GitLastCommitShaColumnDefinition.ActualWidth;
			ColumnsViewModel.TagColumn.UserLengthPixels = Column4.ActualWidth;
			ColumnsViewModel.PathColumn.UserLengthPixels = Column5.ActualWidth;
			ColumnsViewModel.OriginalPathColumn.UserLengthPixels = Column6.ActualWidth;
			ColumnsViewModel.DateDeletedColumn.UserLengthPixels = Column7.ActualWidth;
			ColumnsViewModel.DateModifiedColumn.UserLengthPixels = Column8.ActualWidth;
			ColumnsViewModel.DateCreatedColumn.UserLengthPixels = Column9.ActualWidth;
			ColumnsViewModel.ItemTypeColumn.UserLengthPixels = Column10.ActualWidth;
			ColumnsViewModel.SizeColumn.UserLengthPixels = Column11.ActualWidth;
			ColumnsViewModel.StatusColumn.UserLengthPixels = Column12.ActualWidth;
		}

		public void SizeColumnToFit(DetailsLayoutColumnKind columnToResize)
		{
			if (!FileList.Items.Any())
				return;

			var maxItemLength = columnToResize switch
			{
				DetailsLayoutColumnKind.Icon => 40,
				DetailsLayoutColumnKind.Name => FileList.Items.Cast<ListedItem>().Select(x => x.Name?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.GitStatus => FileList.Items.Cast<ListedItem>().Select(x => (x as GitItem)?.UnmergedGitStatusName?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.GitLastCommitDate => FileList.Items.Cast<ListedItem>().Select(x => (x as GitItem)?.GitLastCommitDateHumanized?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.GitLastCommitMessage => FileList.Items.Cast<ListedItem>().Select(x => (x as GitItem)?.GitLastCommitMessage?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.GitCommitAuthor => FileList.Items.Cast<ListedItem>().Select(x => (x as GitItem)?.GitLastCommitAuthor?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.GitLastCommitSha => FileList.Items.Cast<ListedItem>().Select(x => (x as GitItem)?.GitLastCommitSha?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.Tags => FileList.Items.Cast<ListedItem>().Select(x => x.FileTagsUI?.Sum(x => x?.Name?.Length ?? 0) ?? 0).Max(),
				DetailsLayoutColumnKind.Path => FileList.Items.Cast<ListedItem>().Select(x => x.ItemPath?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.OriginalPath => FileList.Items.Cast<ListedItem>().Select(x => (x as RecycleBinItem)?.ItemOriginalPath?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.DateDeleted => FileList.Items.Cast<ListedItem>().Select(x => (x as RecycleBinItem)?.ItemDateDeleted?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.DateModified => FileList.Items.Cast<ListedItem>().Select(x => x.ItemDateModified?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.DateCreated => FileList.Items.Cast<ListedItem>().Select(x => x.ItemDateCreated?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.Type => FileList.Items.Cast<ListedItem>().Select(x => x.ItemType?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.Size => FileList.Items.Cast<ListedItem>().Select(x => x.FileSize?.Length ?? 0).Max(),
				DetailsLayoutColumnKind.CloudSyncStatus => 20
			};

			// if called programmatically, the column could be hidden
			// in this case, resizing doesn't need to be done at all
			if (maxItemLength == 0)
				return;

			var columnSizeToFit = MeasureColumnEstimate(columnToResize, 5, maxItemLength);

			if (columnSizeToFit > 1)
			{
				var column = columnToResize switch
				{
					DetailsLayoutColumnKind.Name  => GetColumnItem(DetailsLayoutColumnKind.Name),
					DetailsLayoutColumnKind.GitStatus => GetColumnItem(DetailsLayoutColumnKind.GitStatus),
					DetailsLayoutColumnKind.GitLastCommitDate  => GetColumnItem(DetailsLayoutColumnKind.GitLastCommitDate),
					DetailsLayoutColumnKind.GitLastCommitMessage  => GetColumnItem(DetailsLayoutColumnKind.GitLastCommitMessage),
					DetailsLayoutColumnKind.GitCommitAuthor  => GetColumnItem(DetailsLayoutColumnKind.GitCommitAuthor),
					DetailsLayoutColumnKind.GitLastCommitSha  => GetColumnItem(DetailsLayoutColumnKind.GitLastCommitSha),
					DetailsLayoutColumnKind.Tags  => GetColumnItem(DetailsLayoutColumnKind.Tags),
					DetailsLayoutColumnKind.Path  => GetColumnItem(DetailsLayoutColumnKind.Path),
					DetailsLayoutColumnKind.OriginalPath => GetColumnItem(DetailsLayoutColumnKind.OriginalPath),
					DetailsLayoutColumnKind.DateDeleted => GetColumnItem(DetailsLayoutColumnKind.DateDeleted),
					DetailsLayoutColumnKind.DateModified => GetColumnItem(DetailsLayoutColumnKind.DateModified),
					DetailsLayoutColumnKind.DateCreated => GetColumnItem(DetailsLayoutColumnKind.DateCreated),
					DetailsLayoutColumnKind.Type => GetColumnItem(DetailsLayoutColumnKind.Type),
					DetailsLayoutColumnKind.Size => GetColumnItem(DetailsLayoutColumnKind.Size),
					DetailsLayoutColumnKind.CloudSyncStatus => GetColumnItem(DetailsLayoutColumnKind.CloudSyncStatus)
				};

				// File name column
				if (columnToResize == DetailsLayoutColumnKind.Name)
					columnSizeToFit += 20;

				var minFitLength = Math.Max(columnSizeToFit, column.NormalMinLength);

				// 36 to account for SortIcon & padding
				var maxFitLength = Math.Min(minFitLength + 36, column.NormalMaxLength);

				column.UserLengthPixels = maxFitLength;
			}

			LayoutPreferencesManager.ColumnItems.Clear();

			foreach (var item in ColumnItems)
				LayoutPreferencesManager.ColumnItems.Add(item);
		}

		private double MeasureColumnEstimate(int columnIndex, int measureItemsCount, int maxItemLength)
		{
			// sync status
			if (columnIndex == 15)
				return maxItemLength;

			// file tag
			if (columnIndex == 8)
				return MeasureTagColumnEstimate(columnIndex);

			return MeasureTextColumnEstimate(columnIndex, measureItemsCount, maxItemLength);
		}

		private double MeasureTagColumnEstimate(int columnIndex)
		{
			var grids = DependencyObjectHelpers
				.FindChildren<Grid>(FileList.ItemsPanelRoot)
				.Where(grid => IsCorrectColumn(grid, columnIndex));

			// Get the list of stack panels with the most letters
			var stackPanels = grids
				.Select(DependencyObjectHelpers.FindChildren<StackPanel>)
				.OrderByDescending(sps => sps.Select(sp => DependencyObjectHelpers.FindChildren<TextBlock>(sp).Select(tb => tb.Text.Length).Sum()).Sum())
				.First()
				.ToArray();

			var measuredSize = stackPanels.Select(x =>
			{
				x.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

				return x.DesiredSize.Width;
			}).Sum();

			if (stackPanels.Length >= 2)
				measuredSize += 4 * (stackPanels.Length - 1); // The spacing between the tags

			return measuredSize;
		}

		private double MeasureTextColumnEstimate(int columnIndex, int measureItemsCount, int maxItemLength)
		{
			var tbs = DependencyObjectHelpers
				.FindChildren<TextBlock>(FileList.ItemsPanelRoot)
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

		private void ExecuteUpdateSortOptionsCommand(string? sortOptionString)
		{
			if (!Enum.TryParse<SortOption>(sortOptionString, out var sortOption))
				return;

			if (LayoutPreferencesManager.DirectorySortOption == sortOption)
			{
				LayoutPreferencesManager.DirectorySortDirection =
					(SortDirection)(((int)LayoutPreferencesManager.DirectorySortDirection + 1) % 2);
			}
			else
			{
				LayoutPreferencesManager.DirectorySortOption = sortOption;
				LayoutPreferencesManager.DirectorySortDirection = SortDirection.Ascending;
			}
		}

		private void ExecuteToggleColumnVisibilityCommand()
		{
			LayoutPreferencesManager.ColumnItems.Clear();

			foreach (var item in ColumnItems)
				LayoutPreferencesManager.ColumnItems.Add(item);

			ContentPageContext.ShellPage!.FilesystemViewModel.EnabledGitProperties = GetEnabledGitColumns();
		}

		private GitProperties GetEnabledGitColumns()
		{
			var enableStatus =
				!(GetColumnItem(DetailsLayoutColumnKind.GitStatus)?.IsHidden ?? false) &&
				!(GetColumnItem(DetailsLayoutColumnKind.GitStatus)?.UserCollapsed ?? false);

			var enableCommit =
				!(GetColumnItem(DetailsLayoutColumnKind.GitLastCommitDate)?.IsHidden ?? false) &&
				!(GetColumnItem(DetailsLayoutColumnKind.GitLastCommitDate)?.UserCollapsed ?? false) ||
				!(GetColumnItem(DetailsLayoutColumnKind.GitLastCommitMessage)?.IsHidden ?? false) &&
				!(GetColumnItem(DetailsLayoutColumnKind.GitLastCommitMessage)?.UserCollapsed ?? false) ||
				!(GetColumnItem(DetailsLayoutColumnKind.GitCommitAuthor)?.IsHidden ?? false) &&
				!(GetColumnItem(DetailsLayoutColumnKind.GitCommitAuthor)?.UserCollapsed ?? false) ||
				!(GetColumnItem(DetailsLayoutColumnKind.GitLastCommitSha)?.IsHidden ?? false) &&
				!(GetColumnItem(DetailsLayoutColumnKind.GitLastCommitSha)?.UserCollapsed?? false) ;

			return (enableStatus, enableCommit) switch
			{
				(true, true) => GitProperties.All,
				(true, false) => GitProperties.Status,
				(false, true) => GitProperties.Commit,
				(false, false) => GitProperties.None
			};
		}

		private DetailsLayoutColumnItem? GetColumnItem(DetailsLayoutColumnKind? columnKind)
		{
			if (columnKind is null)
				return null;

			return ColumnItems.Where(x => x.Kind == (DetailsLayoutColumnKind)columnKind).FirstOrDefault();
		}
	}
}
