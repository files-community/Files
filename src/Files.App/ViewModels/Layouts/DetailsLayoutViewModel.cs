// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using System.Windows.Input;

namespace Files.App.ViewModels.Layouts
{
	public class DetailsLayoutViewModel : ObservableObject
	{
		// Dependency injections

		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		// Properties

		public ObservableCollection<DetailsLayoutColumnItem> ColumnItems { get; }

		private LayoutPreferencesManager LayoutPreferencesManager
			=> ContentPageContext.ShellPage!.InstanceViewModel.LayoutPreferencesManager;

		private ItemViewModel FilesystemViewModel
			=> ContentPageContext.ShellPage!.FilesystemViewModel;

		// Dependency injections

		public ICommand UpdateSortOptionsCommand { get; }
		public ICommand ToggleColumnVisibilityCommand { get; }

		#region TODO: REMOVE THOSE LINES ASAP

		public DetailsLayoutColumnItem IconColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.Icon)!;

		public DetailsLayoutColumnItem NameColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.Name)!;

		public DetailsLayoutColumnItem GitStatusColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.GitStatus)!;

		public DetailsLayoutColumnItem GitLastCommitDateColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.GitLastCommitDate)!;

		public DetailsLayoutColumnItem GitLastCommitMessageColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.GitLastCommitMessage)!;

		public DetailsLayoutColumnItem GitCommitAuthorColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.GitCommitAuthor)!;

		public DetailsLayoutColumnItem GitLastCommitShaColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.GitLastCommitSha)!;

		public DetailsLayoutColumnItem TagColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.Tags)!;

		public DetailsLayoutColumnItem StatusColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.CloudSyncStatus)!;

		public DetailsLayoutColumnItem DateModifiedColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.DateModified)!;

		public DetailsLayoutColumnItem PathColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.Path)!;

		public DetailsLayoutColumnItem OriginalPathColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.OriginalPath)!;

		public DetailsLayoutColumnItem ItemTypeColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.Type)!;

		public DetailsLayoutColumnItem DateDeletedColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.DateDeleted)!;

		public DetailsLayoutColumnItem DateCreatedColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.DateCreated)!;

		public DetailsLayoutColumnItem SizeColumn
			=> ColumnItems.FirstOrDefault(x => x.Kind == DetailsLayoutColumnKind.Size)!;

		#endregion

		// Constructor

		public DetailsLayoutViewModel()
		{
			ColumnItems = new();

			InitializeDetailsLayoutColumnItems();

			UpdateSortOptionsCommand = new RelayCommand<string>(ExecuteUpdateSortOptionsCommand);
			ToggleColumnVisibilityCommand = new RelayCommand(ExecuteToggleColumnVisibilityCommand);
		}

		// Methods

		public void InitializeDetailsLayoutColumnItems()
		{
			foreach (var item in LayoutPreferencesManager.ColumnItems)
				ColumnItems.Add((DetailsLayoutColumnItem)item);

			FilesystemViewModel.EnabledGitProperties = GetEnabledGitColumns();
		}

		public void UpdateDetailsLayoutColumnsVisibilities(PageTypeUpdatedEventArgs e)
		{
			// Recycle Bin Page
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

			// Cloud Drive Page
			if (e.IsTypeCloudDrive)
			{
				GetColumnItem(DetailsLayoutColumnKind.CloudSyncStatus)?.Show();
			}
			else
			{
				GetColumnItem(DetailsLayoutColumnKind.CloudSyncStatus)?.Hide();
			}

			// Git Repository Page
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

			// Search Page
			if (e.IsTypeSearchResults)
			{
				GetColumnItem(DetailsLayoutColumnKind.Path)?.Show();
			}
			else
			{
				GetColumnItem(DetailsLayoutColumnKind.Path)?.Hide();
			}

			UpdateSortIndicator();
		}

		public void UpdateSortIndicator()
		{
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

				// Reset all sort direction
				ColumnItems.ForEach(x => x.SortDirection = null);

				item.SortDirection = LayoutPreferencesManager.DirectorySortDirection;

				return true;
			}
		}

		public GitProperties GetEnabledGitColumns()
		{
			var gitStatus = (GetColumnItem(DetailsLayoutColumnKind.GitStatus));
			var gitCommitDate = (GetColumnItem(DetailsLayoutColumnKind.GitLastCommitDate));
			var gitCommitMessage = (GetColumnItem(DetailsLayoutColumnKind.GitLastCommitMessage));
			var gitCommitAuthor = (GetColumnItem(DetailsLayoutColumnKind.GitCommitAuthor));
			var gitCommitSha = (GetColumnItem(DetailsLayoutColumnKind.GitLastCommitSha));

			var enableStatus =
				(gitStatus?.IsAvailable ?? false) &&
				(gitStatus?.IsVisible ?? false);

			var enableCommit =
				(gitCommitDate?.IsAvailable ?? false) &&
				(gitCommitDate?.IsVisible ?? false) ||
				(gitCommitMessage?.IsAvailable ?? false) &&
				(gitCommitMessage?.IsVisible ?? false) ||
				(gitCommitAuthor?.IsAvailable ?? false) &&
				(gitCommitAuthor?.IsVisible ?? false) ||
				(gitCommitSha?.IsAvailable ?? false) &&
				(gitCommitSha?.IsVisible ?? false);

			return (enableStatus, enableCommit) switch
			{
				(true, true) => GitProperties.All,
				(true, false) => GitProperties.Status,
				(false, true) => GitProperties.Commit,
				(false, false) => GitProperties.None
			};
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

		private DetailsLayoutColumnItem? GetColumnItem(DetailsLayoutColumnKind? columnKind)
		{
			if (columnKind is null)
				return null;

			return ColumnItems.Where(x => x.Kind == (DetailsLayoutColumnKind)columnKind).FirstOrDefault();
		}
	}
}
