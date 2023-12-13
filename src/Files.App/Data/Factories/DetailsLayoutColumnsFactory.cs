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

			// Set static settings
			GetColumnItem(columns, DetailsLayoutColumnKind.Name).MaxWidth = 1000;
			GetColumnItem(columns, DetailsLayoutColumnKind.Path).MaxWidth = 500;
			GetColumnItem(columns, DetailsLayoutColumnKind.OriginalPath).MaxWidth = 500;
			GetColumnItem(columns, DetailsLayoutColumnKind.Icon);

			// Set column names
			GetColumnItem(columns, DetailsLayoutColumnKind.Name).Name = "Name";
			GetColumnItem(columns, DetailsLayoutColumnKind.GitStatus).Name = "GitStatus";
			GetColumnItem(columns, DetailsLayoutColumnKind.GitLastCommitDate).Name = "DateCommitted";
			GetColumnItem(columns, DetailsLayoutColumnKind.GitLastCommitMessage).Name = "CommitMessage";
			GetColumnItem(columns, DetailsLayoutColumnKind.GitCommitAuthor).Name = "Author";
			GetColumnItem(columns, DetailsLayoutColumnKind.GitLastCommitSha).Name = "CommitSha";
			GetColumnItem(columns, DetailsLayoutColumnKind.Tags).Name = "Tag";
			GetColumnItem(columns, DetailsLayoutColumnKind.Path).Name = "Path";
			GetColumnItem(columns, DetailsLayoutColumnKind.OriginalPath).Name = "OriginalPath";
			GetColumnItem(columns, DetailsLayoutColumnKind.DateDeleted).Name = "DateDeleted";
			GetColumnItem(columns, DetailsLayoutColumnKind.DateModified).Name = "DateModifiedLowerCase";
			GetColumnItem(columns, DetailsLayoutColumnKind.DateCreated).Name = "DateCreated";
			GetColumnItem(columns, DetailsLayoutColumnKind.Type).Name = "Type";
			GetColumnItem(columns, DetailsLayoutColumnKind.Size).Name = "Size";
			GetColumnItem(columns, DetailsLayoutColumnKind.CloudSyncStatus).Name = "syncStatusColumn";

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
