// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents item for a folder's layout preferences.
	/// </summary>
	public sealed class LayoutPreferencesItem
	{
		// Dependency injections

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Properties

		public ColumnsViewModel ColumnsViewModel { get; set; }

		public bool SortDirectoriesAlongsideFiles { get; set; }
		public bool SortFilesFirst { get; set; }
		public bool IsAdaptiveLayoutOverridden { get; set; }

		public FolderLayoutModes LayoutMode { get; set; }

		public SortOption DirectorySortOption { get; set; }
		private string _DirectorySortColumnId = string.Empty;
		public string DirectorySortColumnId
		{
			get => _DirectorySortColumnId;
			set => _DirectorySortColumnId = value ?? string.Empty;
		}
		public SortDirection DirectorySortDirection { get; set; }
		public SortDirection DirectoryGroupDirection { get; set; }

		public GroupOption DirectoryGroupOption { get; set; }

		private string _DirectoryGroupColumnId = string.Empty;
		public string DirectoryGroupColumnId
		{
			get => _DirectoryGroupColumnId;
			set => _DirectoryGroupColumnId = value ?? string.Empty;
		}
		public GroupByDateUnit DirectoryGroupByDateUnit { get; set; }

		// Constructor

		public LayoutPreferencesItem()
		{
			var defaultLayout = UserSettingsService.LayoutSettingsService.DefaultLayoutMode;

			LayoutMode = defaultLayout is FolderLayoutModes.Adaptive ? FolderLayoutModes.DetailsView : defaultLayout;
			DirectorySortOption = UserSettingsService.LayoutSettingsService.DefaultSortOption;
			DirectorySortColumnId = UserSettingsService.LayoutSettingsService.DefaultSortColumnId;
			DirectoryGroupOption = UserSettingsService.LayoutSettingsService.DefaultGroupOption;
			DirectoryGroupColumnId = UserSettingsService.LayoutSettingsService.DefaultGroupColumnId;
			DirectorySortDirection = UserSettingsService.LayoutSettingsService.DefaultDirectorySortDirection;
			DirectoryGroupDirection = UserSettingsService.LayoutSettingsService.DefaultDirectoryGroupDirection;
			DirectoryGroupByDateUnit = UserSettingsService.LayoutSettingsService.DefaultGroupByDateUnit;
			SortDirectoriesAlongsideFiles = UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles;
			SortFilesFirst = UserSettingsService.LayoutSettingsService.DefaultSortFilesFirst;
			IsAdaptiveLayoutOverridden = defaultLayout is not FolderLayoutModes.Adaptive;

			ColumnsViewModel = new ColumnsViewModel();
			DetailsLayoutColumnDefaults.Apply(ColumnsViewModel);
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
				return (
					item.LayoutMode == LayoutMode &&
					item.DirectoryGroupOption == DirectoryGroupOption &&
					item.DirectoryGroupColumnId == DirectoryGroupColumnId &&
					item.DirectorySortOption == DirectorySortOption &&
					item.DirectorySortColumnId == DirectorySortColumnId &&
					item.DirectorySortDirection == DirectorySortDirection &&
					item.DirectoryGroupDirection == DirectoryGroupDirection &&
					item.DirectoryGroupByDateUnit == DirectoryGroupByDateUnit &&
					item.SortDirectoriesAlongsideFiles == SortDirectoriesAlongsideFiles &&
					item.SortFilesFirst == SortFilesFirst &&
					item.IsAdaptiveLayoutOverridden == IsAdaptiveLayoutOverridden &&
					item.ColumnsViewModel.Equals(ColumnsViewModel));
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			HashCode hash = new();

			hash.Add(LayoutMode);
			hash.Add(DirectoryGroupOption);
			hash.Add(DirectoryGroupColumnId);
			hash.Add(DirectorySortOption);
			hash.Add(DirectorySortColumnId);
			hash.Add(DirectorySortDirection);
			hash.Add(DirectoryGroupDirection);
			hash.Add(DirectoryGroupByDateUnit);
			hash.Add(SortDirectoriesAlongsideFiles);
			hash.Add(SortFilesFirst);
			hash.Add(IsAdaptiveLayoutOverridden);
			hash.Add(ColumnsViewModel);

			return hash.ToHashCode();
		}
	}
}
