// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents a column offered in the sort or group menus beside the built-in options.
	/// </summary>
	/// <param name="IsDate">Whether the values are dates, which can be grouped by year, month or day.</param>
	public sealed record FolderColumnOption(string Id, string Label, bool IsChecked, bool IsDate);

	/// <summary>
	/// Provides the columns of the current folder that items can be sorted and grouped by, beside the built-in options.
	/// </summary>
	public static class FolderColumnOptions
	{
		private static readonly string[] GitGroupColumnIds = [DetailsLayoutColumnIds.GitStatus, DetailsLayoutColumnIds.GitCommitAuthor, DetailsLayoutColumnIds.GitLastCommitDate];

		public static IReadOnlyList<FolderColumnOption> GetSortOptions(CurrentInstanceViewModel instanceViewModel, ShellViewModel? shellViewModel)
		{
			var folderSettings = instanceViewModel.FolderSettings;
			var checkedId = folderSettings.DirectorySortOption is SortOption.Property ? folderSettings.DirectorySortPropertyId : null;
			var gitIds = DetailsLayoutColumnDefinitions.BuiltIn.Where(definition => definition.IsGitColumn).Select(definition => definition.Id);
			return GetOptions(instanceViewModel, shellViewModel, gitIds, column => true, checkedId);
		}

		public static IReadOnlyList<FolderColumnOption> GetGroupOptions(CurrentInstanceViewModel instanceViewModel, ShellViewModel? shellViewModel)
		{
			var folderSettings = instanceViewModel.FolderSettings;
			var checkedId = folderSettings.DirectoryGroupOption is GroupOption.Property ? folderSettings.DirectoryGroupPropertyId : null;
			return GetOptions(instanceViewModel, shellViewModel, GitGroupColumnIds, column => column.CanGroup, checkedId);
		}

		public static IEnumerable<(GroupByDateUnit Unit, string Label)> GetDateUnits()
		{
			yield return (GroupByDateUnit.Year, Strings.Year.GetLocalizedResource());
			yield return (GroupByDateUnit.Month, Strings.Month.GetLocalizedResource());
			yield return (GroupByDateUnit.Day, Strings.Day.GetLocalizedResource());
		}

		public static void SortBy(LayoutPreferencesManager folderSettings, string id)
		{
			folderSettings.SetSortProperty(id);
			LayoutHelpers.UpdateOpenTabsPreferences();
		}

		public static void GroupBy(LayoutPreferencesManager folderSettings, string id, GroupByDateUnit? dateUnit = null)
		{
			if (dateUnit is { } unit)
				folderSettings.DirectoryGroupByDateUnit = unit;

			folderSettings.SetGroupProperty(id);
			LayoutHelpers.UpdateOpenTabsPreferences();
		}

		private static IReadOnlyList<FolderColumnOption> GetOptions(CurrentInstanceViewModel instanceViewModel, ShellViewModel? shellViewModel, IEnumerable<string> gitIds, Func<ShellFolderColumn, bool> folderColumnFilter, string? checkedId)
		{
			var catalog = ShellPropertyCatalog.GetAsync() is { IsCompletedSuccessfully: true } catalogTask ? catalogTask.Result : null;
			var folderColumns = ShellFolderColumns.GetAsync(shellViewModel?.WorkingDirectory) is { IsCompletedSuccessfully: true } columnsTask ? columnsTask.Result : [];
			var columnsViewModel = instanceViewModel.FolderSettings.ColumnsViewModel;

			var ids = columnsViewModel.OrderedColumns.Ids
				.Where(id => ColumnValueHelper.IsShellPropertyColumn(id) && columnsViewModel.IsColumnVisible(id))
				.Concat(folderColumns.Where(folderColumnFilter).Select(column => column.Id).Where(ColumnValueHelper.IsShellPropertyColumn));

			if (shellViewModel is { IsValidGitDirectory: true } && !instanceViewModel.IsPageTypeSearchResults)
				ids = ids.Concat(gitIds);

			if (checkedId is { Length: > 0 })
				ids = ids.Append(checkedId);

			return ids
				.Distinct()
				.Select(id => new FolderColumnOption(
					id,
					DetailsLayoutColumnDefaults.GetDisplayName(id, catalog),
					id == checkedId,
					id == DetailsLayoutColumnIds.GitLastCommitDate || (catalog is not null && catalog.TryGetValue(id, out var info) && info.IsDate)))
				.ToArray();
		}
	}
}
