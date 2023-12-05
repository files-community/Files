// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents item for a folder's layout preferences.
	/// </summary>
	public class LayoutPreferencesItem
	{
		// Fields

		public IList<IDetailsLayoutColumnItem> ColumnItems;

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
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			var defaultLayout = userSettingsService.FoldersSettingsService.DefaultLayoutMode;

			SortDirectoriesAlongsideFiles = userSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles;
			IsAdaptiveLayoutOverridden = defaultLayout is not FolderLayoutModes.Adaptive;
			GridViewSize = userSettingsService.LayoutSettingsService.DefaultGridViewSize;

			LayoutMode = defaultLayout is FolderLayoutModes.Adaptive ? FolderLayoutModes.DetailsView : defaultLayout;

			DirectorySortOption = userSettingsService.FoldersSettingsService.DefaultSortOption;
			DirectorySortDirection = userSettingsService.FoldersSettingsService.DefaultDirectorySortDirection;

			DirectoryGroupOption = userSettingsService.FoldersSettingsService.DefaultGroupOption;
			DirectoryGroupDirection = userSettingsService.FoldersSettingsService.DefaultDirectoryGroupDirection;
			DirectoryGroupByDateUnit = userSettingsService.FoldersSettingsService.DefaultGroupByDateUnit;

			ColumnItems = new List<IDetailsLayoutColumnItem>();
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
