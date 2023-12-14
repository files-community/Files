// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Factories
{
	public static class DetailsLayoutColumnsFactory
	{
		public static IList<DetailsLayoutColumnItem> GenerateItems()
		{
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			var columns =
				userSettingsService.LayoutSettingsService.ColumnItems?.Select(DetailsLayoutColumnItem.ToItem).ToList()
				?? GenerateDefaultItems();

			// Set column names
			GetColumnItem(columns, DetailsLayoutColumnKind.Name).Name = "Name".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.GitStatus).Name = "GitStatus".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.GitLastCommitDate).Name = "DateCommitted".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.GitLastCommitMessage).Name = "CommitMessage".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.GitCommitAuthor).Name = "Author".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.GitLastCommitSha).Name = "CommitSha".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.Tags).Name = "Tag".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.Path).Name = "Path".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.OriginalPath).Name = "OriginalPath".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.DateDeleted).Name = "DateDeleted".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.DateModified).Name = "DateModifiedLowerCase".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.DateCreated).Name = "DateCreated".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.Type).Name = "Type".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.Size).Name = "Size".GetLocalizedResource();
			GetColumnItem(columns, DetailsLayoutColumnKind.CloudSyncStatus).Name = "syncStatusColumn".GetLocalizedResource();

			return columns;
		}

		public static IList<DetailsLayoutColumnItem> GenerateDefaultItems()
		{
			List<DetailsLayoutColumnItem> columns = new()
			{
				new(DetailsLayoutColumnKind.Icon, 24, true),
				new(DetailsLayoutColumnKind.Name, 240, true) { MaxWidth = 1000 },
				new(DetailsLayoutColumnKind.GitStatus, 80, false),
				new(DetailsLayoutColumnKind.GitLastCommitDate, 140, false),
				new(DetailsLayoutColumnKind.GitLastCommitMessage, 140, false),
				new(DetailsLayoutColumnKind.GitCommitAuthor, 140, false),
				new(DetailsLayoutColumnKind.GitLastCommitSha, 80, false),
				new(DetailsLayoutColumnKind.Tags, 140, true),
				new(DetailsLayoutColumnKind.Path, 200, true) { MaxWidth = 500 },
				new(DetailsLayoutColumnKind.OriginalPath, 200, true) { MaxWidth = 500 },
				new(DetailsLayoutColumnKind.DateDeleted, 200, true),
				new(DetailsLayoutColumnKind.DateModified, 200, true),
				new(DetailsLayoutColumnKind.DateCreated, 200, false),
				new(DetailsLayoutColumnKind.Type, 140, true),
				new(DetailsLayoutColumnKind.Size, 100, true),
				new(DetailsLayoutColumnKind.CloudSyncStatus, 50, false) { MaxWidth = 80 },
			};

			return columns;
		}

		private static DetailsLayoutColumnItem GetColumnItem(IList<DetailsLayoutColumnItem> columns, DetailsLayoutColumnKind columnKind)
		{
			return columns.Where(x => x.Kind == columnKind).FirstOrDefault()!;
		}
	}
}
