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
		}

		public void SizeColumnToFit(DetailsLayoutColumnKind columnToResize)
		{
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
