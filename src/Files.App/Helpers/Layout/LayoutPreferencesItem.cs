// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents item for a folder's layout preferences.
	/// </summary>
	public class LayoutPreferencesItem
	{
		// Dependency injections

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Fields

		public IList<DetailsLayoutColumnItem> ColumnItems;

		public bool SortDirectoriesAlongsideFiles;
		public bool IsAdaptiveLayoutOverridden;
		public int GridViewSize;

		public FolderLayoutModes LayoutMode;

		public SortOption DirectorySortOption;
		public SortDirection DirectorySortDirection;
		public SortDirection DirectoryGroupDirection;

		public GroupOption DirectoryGroupOption;
		public GroupByDateUnit DirectoryGroupByDateUnit;

		// Constructor

		public LayoutPreferencesItem()
		{
			var defaultLayout = UserSettingsService.FoldersSettingsService.DefaultLayoutMode;

			LayoutMode = defaultLayout is FolderLayoutModes.Adaptive ? FolderLayoutModes.DetailsView : defaultLayout;
			GridViewSize = UserSettingsService.LayoutSettingsService.DefaultGridViewSize;
			DirectorySortOption = UserSettingsService.FoldersSettingsService.DefaultSortOption;
			DirectoryGroupOption = UserSettingsService.FoldersSettingsService.DefaultGroupOption;
			DirectorySortDirection = UserSettingsService.FoldersSettingsService.DefaultDirectorySortDirection;
			DirectoryGroupDirection = UserSettingsService.FoldersSettingsService.DefaultDirectoryGroupDirection;
			DirectoryGroupByDateUnit = UserSettingsService.FoldersSettingsService.DefaultGroupByDateUnit;
			SortDirectoriesAlongsideFiles = UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles;
			IsAdaptiveLayoutOverridden = defaultLayout is not FolderLayoutModes.Adaptive;

			ColumnItems = new List<DetailsLayoutColumnItem>();
			var generatedColumns = DetailsLayoutColumnsFactory.GenerateItems();

			foreach (var item in generatedColumns)
				ColumnItems.Add(item);
		}

		// Overridden methods

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is LayoutPreferencesItem item)
			{
				return
					item.LayoutMode == LayoutMode &&
					item.GridViewSize == GridViewSize &&
					item.DirectoryGroupOption == DirectoryGroupOption &&
					item.DirectorySortOption == DirectorySortOption &&
					item.DirectorySortDirection == DirectorySortDirection &&
					item.DirectoryGroupDirection == DirectoryGroupDirection &&
					item.DirectoryGroupByDateUnit == DirectoryGroupByDateUnit &&
					item.SortDirectoriesAlongsideFiles == SortDirectoriesAlongsideFiles &&
					item.IsAdaptiveLayoutOverridden == IsAdaptiveLayoutOverridden;
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			HashCode hash = new();

			hash.Add(LayoutMode);
			hash.Add(GridViewSize);
			hash.Add(DirectoryGroupOption);
			hash.Add(DirectorySortOption);
			hash.Add(DirectorySortDirection);
			hash.Add(DirectoryGroupDirection);
			hash.Add(DirectoryGroupByDateUnit);
			hash.Add(SortDirectoriesAlongsideFiles);
			hash.Add(IsAdaptiveLayoutOverridden);
			hash.Add(ColumnItems);

			return hash.ToHashCode();
		}
	}
}
