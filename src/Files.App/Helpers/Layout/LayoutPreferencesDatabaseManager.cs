// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Diagnostics.CodeAnalysis;

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents manager for the database of layout preferences.
	/// </summary>
	public class LayoutPreferencesDatabaseManager
	{
		// Fields
		private readonly Server.Database.LayoutPreferencesDatabase _database = new();

		private DetailsLayoutColumnItem FromDatabaseEntity(Server.Data.ColumnPreferencesItem entry)
		{
			return new()
			{
				NormalMaxLength = entry.NormalMaxLength,
				UserCollapsed = entry.UserCollapsed,
				UserLengthPixels = entry.UserLengthPixels,
			};
		}

		[return: NotNullIfNotNull(nameof(entry))]
		private LayoutPreferencesItem? FromDatabaseEntity(Server.Data.LayoutPreferencesItem? entry)
		{
			if (entry is null)
			{
				return null;
			}

			return new()
			{
				ColumnsViewModel =
				{
					DateCreatedColumn = FromDatabaseEntity(entry.ColumnsViewModel.DateCreatedColumn),
					DateDeletedColumn = FromDatabaseEntity(entry.ColumnsViewModel.DateDeletedColumn),
					DateModifiedColumn = FromDatabaseEntity(entry.ColumnsViewModel.DateModifiedColumn),
					GitCommitAuthorColumn = FromDatabaseEntity(entry.ColumnsViewModel.GitCommitAuthorColumn),
					GitLastCommitDateColumn = FromDatabaseEntity(entry.ColumnsViewModel.GitLastCommitDateColumn),
					GitLastCommitMessageColumn = FromDatabaseEntity(entry.ColumnsViewModel.GitLastCommitMessageColumn),
					GitLastCommitShaColumn = FromDatabaseEntity(entry.ColumnsViewModel.GitLastCommitShaColumn),
					GitStatusColumn = FromDatabaseEntity(entry.ColumnsViewModel.GitStatusColumn),
					ItemTypeColumn = FromDatabaseEntity(entry.ColumnsViewModel.ItemTypeColumn),
					NameColumn = FromDatabaseEntity(entry.ColumnsViewModel.NameColumn),
					OriginalPathColumn = FromDatabaseEntity(entry.ColumnsViewModel.OriginalPathColumn),
					PathColumn = FromDatabaseEntity(entry.ColumnsViewModel.PathColumn),
					SizeColumn = FromDatabaseEntity(entry.ColumnsViewModel.SizeColumn),
					StatusColumn = FromDatabaseEntity(entry.ColumnsViewModel.StatusColumn),
					TagColumn = FromDatabaseEntity(entry.ColumnsViewModel.TagColumn),
				},
				DirectoryGroupByDateUnit = entry.DirectoryGroupByDateUnit,
				DirectoryGroupDirection = entry.DirectoryGroupDirection,
				DirectoryGroupOption = entry.DirectoryGroupOption,
				DirectorySortDirection = entry.DirectorySortDirection,
				DirectorySortOption = entry.DirectorySortOption,
				IsAdaptiveLayoutOverridden = entry.IsAdaptiveLayoutOverridden,
				LayoutMode = entry.LayoutMode,
				SortDirectoriesAlongsideFiles = entry.SortDirectoriesAlongsideFiles,
				SortFilesFirst = entry.SortFilesFirst,
			};
		}

		private Server.Data.ColumnPreferencesItem ToDatabaseEntity(DetailsLayoutColumnItem entry)
		{
			return new()
			{
				NormalMaxLength = entry.NormalMaxLength,
				UserCollapsed = entry.UserCollapsed,
				UserLengthPixels = entry.UserLengthPixels,
			};
		}

		[return: NotNullIfNotNull(nameof(entry))]
		private Server.Data.LayoutPreferencesItem? ToDatabaseEntity(LayoutPreferencesItem? entry)
		{
			if (entry is null)
			{
				return null;
			}

			return new()
			{
				ColumnsViewModel =
				{
					DateCreatedColumn = ToDatabaseEntity(entry.ColumnsViewModel.DateCreatedColumn),
					DateDeletedColumn = ToDatabaseEntity(entry.ColumnsViewModel.DateDeletedColumn),
					DateModifiedColumn = ToDatabaseEntity(entry.ColumnsViewModel.DateModifiedColumn),
					GitCommitAuthorColumn = ToDatabaseEntity(entry.ColumnsViewModel.GitCommitAuthorColumn),
					GitLastCommitDateColumn = ToDatabaseEntity(entry.ColumnsViewModel.GitLastCommitDateColumn),
					GitLastCommitMessageColumn = ToDatabaseEntity(entry.ColumnsViewModel.GitLastCommitMessageColumn),
					GitLastCommitShaColumn = ToDatabaseEntity(entry.ColumnsViewModel.GitLastCommitShaColumn),
					GitStatusColumn = ToDatabaseEntity(entry.ColumnsViewModel.GitStatusColumn),
					ItemTypeColumn = ToDatabaseEntity(entry.ColumnsViewModel.ItemTypeColumn),
					NameColumn = ToDatabaseEntity(entry.ColumnsViewModel.NameColumn),
					OriginalPathColumn = ToDatabaseEntity(entry.ColumnsViewModel.OriginalPathColumn),
					PathColumn = ToDatabaseEntity(entry.ColumnsViewModel.PathColumn),
					SizeColumn = ToDatabaseEntity(entry.ColumnsViewModel.SizeColumn),
					StatusColumn = ToDatabaseEntity(entry.ColumnsViewModel.StatusColumn),
					TagColumn = ToDatabaseEntity(entry.ColumnsViewModel.TagColumn),
				},
				DirectoryGroupByDateUnit = entry.DirectoryGroupByDateUnit,
				DirectoryGroupDirection = entry.DirectoryGroupDirection,
				DirectoryGroupOption = entry.DirectoryGroupOption,
				DirectorySortDirection = entry.DirectorySortDirection,
				DirectorySortOption = entry.DirectorySortOption,
				IsAdaptiveLayoutOverridden = entry.IsAdaptiveLayoutOverridden,
				LayoutMode = entry.LayoutMode,
				SortDirectoriesAlongsideFiles = entry.SortDirectoriesAlongsideFiles,
				SortFilesFirst = entry.SortFilesFirst,
			};
		}

		// Methods
		public LayoutPreferencesItem? GetPreferences(string filePath, ulong? frn = null)
		{
			return FromDatabaseEntity(_database.GetPreferences(filePath, frn));
		}

		public void SetPreferences(string filePath, ulong? frn, LayoutPreferencesItem? preferencesItem)
		{
			_database.SetPreferences(filePath, frn, ToDatabaseEntity(preferencesItem));
		}

		public void ResetAll()
		{
			_database.ResetAll();
		}

		public void Import(string json)
		{
			_database.Import(json);
		}

		public string Export()
		{
			return _database.Export();
		}
	}
}
