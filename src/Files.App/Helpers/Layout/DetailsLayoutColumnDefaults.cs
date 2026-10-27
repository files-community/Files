// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Globalization;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides the default columns of <see cref="DetailsLayoutPage"/> for folders without saved preferences.
	/// </summary>
	public static class DetailsLayoutColumnDefaults
	{
		private const double DefaultPropertyColumnWidth = 150;

		private static ILayoutSettingsService LayoutSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>().LayoutSettingsService;

		/// <summary>
		/// Gets the visible default columns in display order.
		/// </summary>
		public static IReadOnlyList<string> GetColumns()
		{
			if (LayoutSettingsService.DetailsLayoutColumns is { } columns)
				return [.. columns.Where(IsKnownColumn).Distinct()];

			return [.. DetailsLayoutColumnIds.All.Where(id => id == DetailsLayoutColumnIds.Name || IsShownByLegacySetting(id))];
		}

		public static void SetColumns(IEnumerable<string> columns)
		{
			var visibleColumns = columns.Where(IsKnownColumn).Distinct().ToList();
			if (!visibleColumns.Contains(DetailsLayoutColumnIds.Name))
				visibleColumns.Insert(0, DetailsLayoutColumnIds.Name);

			LayoutSettingsService.DetailsLayoutColumns = visibleColumns;
			LayoutSettingsService.ShowDateColumn = visibleColumns.Contains(DetailsLayoutColumnIds.DateModified);
			LayoutSettingsService.ShowDateCreatedColumn = visibleColumns.Contains(DetailsLayoutColumnIds.DateCreated);
			LayoutSettingsService.ShowTypeColumn = visibleColumns.Contains(DetailsLayoutColumnIds.ItemType);
			LayoutSettingsService.ShowSizeColumn = visibleColumns.Contains(DetailsLayoutColumnIds.Size);
			LayoutSettingsService.ShowFileTagColumn = visibleColumns.Contains(DetailsLayoutColumnIds.Tag);
			LayoutSettingsService.ShowGitStatusColumn = visibleColumns.Contains(DetailsLayoutColumnIds.GitStatus);
			LayoutSettingsService.ShowGitLastCommitDateColumn = visibleColumns.Contains(DetailsLayoutColumnIds.GitLastCommitDate);
			LayoutSettingsService.ShowGitLastCommitMessageColumn = visibleColumns.Contains(DetailsLayoutColumnIds.GitLastCommitMessage);
			LayoutSettingsService.ShowGitCommitAuthorColumn = visibleColumns.Contains(DetailsLayoutColumnIds.GitCommitAuthor);
			LayoutSettingsService.ShowGitLastCommitShaColumn = visibleColumns.Contains(DetailsLayoutColumnIds.GitLastCommitSha);
			LayoutSettingsService.ShowDateDeletedColumn = visibleColumns.Contains(DetailsLayoutColumnIds.DateDeleted);
			LayoutSettingsService.ShowPathColumn = visibleColumns.Contains(DetailsLayoutColumnIds.Path);
			LayoutSettingsService.ShowOriginalPathColumn = visibleColumns.Contains(DetailsLayoutColumnIds.OriginalPath);
			LayoutSettingsService.ShowSyncStatusColumn = visibleColumns.Contains(DetailsLayoutColumnIds.Status);
		}

		/// <summary>
		/// Applies the default columns to the specified columns.
		/// </summary>
		public static void Apply(ColumnsViewModel columns)
		{
			var visibleColumns = GetColumns();

			foreach (var id in DetailsLayoutColumnIds.All)
			{
				if (id is not (DetailsLayoutColumnIds.Icon or DetailsLayoutColumnIds.Name))
					columns.GetColumn(id).UserCollapsed = !visibleColumns.Contains(id);
			}

			var catalog = ShellPropertyCatalog.GetAsync() is { IsCompletedSuccessfully: true } loadTask ? loadTask.Result : null;
			columns.PropertyColumns = [.. visibleColumns
				.Where(ColumnValueHelper.IsShellPropertyColumn)
				.Select(id =>
				{
					var width = catalog is not null && catalog.TryGetValue(id, out var info) ? info.DefaultWidth : DefaultPropertyColumnWidth;
					return $"{id}|{width.ToString(CultureInfo.InvariantCulture)}";
				})];

			columns.ColumnOrder = [.. visibleColumns];
		}

		/// <summary>
		/// Saves the visible columns of the specified columns as the default columns.
		/// </summary>
		/// <remarks>
		/// Columns hidden because they don't apply to the current folder keep their previous default state.
		/// </remarks>
		public static void Save(ColumnsViewModel columns)
		{
			var previousColumns = GetColumns();
			var visibleColumns = columns.OrderedColumns.Ids
				.Where(id => id != DetailsLayoutColumnIds.Icon)
				.Where(id =>
				{
					if (ColumnValueHelper.IsShellPropertyColumn(id))
						return true;

					var column = columns.GetColumn(id);
					return column.IsHidden ? previousColumns.Contains(id) : !column.UserCollapsed;
				});

			SetColumns(visibleColumns);
		}

		public static string GetBuiltInDisplayName(string id)
		{
			return (id switch
			{
				DetailsLayoutColumnIds.Name => Strings.Name,
				DetailsLayoutColumnIds.GitStatus => Strings.GitStatus,
				DetailsLayoutColumnIds.GitLastCommitDate => Strings.DateCommitted,
				DetailsLayoutColumnIds.GitLastCommitMessage => Strings.CommitMessage,
				DetailsLayoutColumnIds.GitCommitAuthor => Strings.Author,
				DetailsLayoutColumnIds.GitLastCommitSha => Strings.CommitSha,
				DetailsLayoutColumnIds.Tag => Strings.Tag,
				DetailsLayoutColumnIds.Path => Strings.Path,
				DetailsLayoutColumnIds.OriginalPath => Strings.OriginalPath,
				DetailsLayoutColumnIds.DateDeleted => Strings.DateDeleted,
				DetailsLayoutColumnIds.DateModified => Strings.DateModifiedLowerCase,
				DetailsLayoutColumnIds.DateCreated => Strings.DateCreated,
				DetailsLayoutColumnIds.ItemType => Strings.Type,
				DetailsLayoutColumnIds.Size => Strings.Size,
				DetailsLayoutColumnIds.Status => Strings.Status,
				_ => id,
			}).GetLocalizedResource();
		}

		private static bool IsKnownColumn(string id)
		{
			return id != DetailsLayoutColumnIds.Icon && (DetailsLayoutColumnIds.All.Contains(id) || ColumnValueHelper.IsShellPropertyColumn(id));
		}

		private static bool IsShownByLegacySetting(string id)
		{
			return id switch
			{
				DetailsLayoutColumnIds.DateModified => LayoutSettingsService.ShowDateColumn,
				DetailsLayoutColumnIds.DateCreated => LayoutSettingsService.ShowDateCreatedColumn,
				DetailsLayoutColumnIds.ItemType => LayoutSettingsService.ShowTypeColumn,
				DetailsLayoutColumnIds.Size => LayoutSettingsService.ShowSizeColumn,
				DetailsLayoutColumnIds.Tag => LayoutSettingsService.ShowFileTagColumn,
				DetailsLayoutColumnIds.GitStatus => LayoutSettingsService.ShowGitStatusColumn,
				DetailsLayoutColumnIds.GitLastCommitDate => LayoutSettingsService.ShowGitLastCommitDateColumn,
				DetailsLayoutColumnIds.GitLastCommitMessage => LayoutSettingsService.ShowGitLastCommitMessageColumn,
				DetailsLayoutColumnIds.GitCommitAuthor => LayoutSettingsService.ShowGitCommitAuthorColumn,
				DetailsLayoutColumnIds.GitLastCommitSha => LayoutSettingsService.ShowGitLastCommitShaColumn,
				DetailsLayoutColumnIds.DateDeleted => LayoutSettingsService.ShowDateDeletedColumn,
				DetailsLayoutColumnIds.Path => LayoutSettingsService.ShowPathColumn,
				DetailsLayoutColumnIds.OriginalPath => LayoutSettingsService.ShowOriginalPathColumn,
				DetailsLayoutColumnIds.Status => LayoutSettingsService.ShowSyncStatusColumn,
				_ => false,
			};
		}
	}
}
