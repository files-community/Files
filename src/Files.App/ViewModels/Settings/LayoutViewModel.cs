// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.Settings
{
	public class LayoutViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// FileTag combobox indexes (required to hide SyncStatus)
		private readonly int FileTagSortingIndex = 5;
		private readonly int FileTagGroupingIndex = 6;

		public LayoutViewModel()
		{
			SelectedDefaultLayoutModeIndex = (int)DefaultLayoutMode;
			SelectedDefaultSortingIndex = UserSettingsService.FoldersSettingsService.DefaultSortOption == SortOption.FileTag ? FileTagSortingIndex : (int)UserSettingsService.FoldersSettingsService.DefaultSortOption;
			SelectedDefaultGroupingIndex = UserSettingsService.FoldersSettingsService.DefaultGroupOption == GroupOption.FileTag ? FileTagGroupingIndex : (int)UserSettingsService.FoldersSettingsService.DefaultGroupOption;
			SelectedDefaultGroupByDateUnitIndex = (int)UserSettingsService.FoldersSettingsService.DefaultGroupByDateUnit;
			SelectedDefaultSortPriorityIndex = UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles ? 2 : UserSettingsService.FoldersSettingsService.DefaultSortFilesFirst ? 1 : 0;
		}

		// Properties

		public bool SyncFolderPreferencesAcrossDirectories
		{
			get => UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories)
				{
					UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories = value;

					ResetLayoutPreferences();
					OnPropertyChanged();
				}
			}
		}

		public bool SortInDescendingOrder
		{
			get => UserSettingsService.FoldersSettingsService.DefaultDirectorySortDirection == SortDirection.Descending;
			set
			{
				if (value != (UserSettingsService.FoldersSettingsService.DefaultDirectorySortDirection == SortDirection.Descending))
				{
					UserSettingsService.FoldersSettingsService.DefaultDirectorySortDirection = value ? SortDirection.Descending : SortDirection.Ascending;
					OnPropertyChanged();
				}
			}
		}

		public bool GroupInDescendingOrder
		{
			get => UserSettingsService.FoldersSettingsService.DefaultDirectoryGroupDirection == SortDirection.Descending;
			set
			{
				if (value != (UserSettingsService.FoldersSettingsService.DefaultDirectoryGroupDirection == SortDirection.Descending))
				{
					UserSettingsService.FoldersSettingsService.DefaultDirectoryGroupDirection = value ? SortDirection.Descending : SortDirection.Ascending;
					OnPropertyChanged();
				}
			}
		}

		public bool IsDefaultGrouped
			=> UserSettingsService.FoldersSettingsService.DefaultGroupOption != GroupOption.None;

		private int defaultGroupByDateUnitIndex;
		public int SelectedDefaultGroupByDateUnitIndex
		{
			get => defaultGroupByDateUnitIndex;
			set
			{
				if (SetProperty(ref defaultGroupByDateUnitIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultGroupByDateUnitIndex));
					UserSettingsService.FoldersSettingsService.DefaultGroupByDateUnit = (GroupByDateUnit)value;
				}
			}
		}

		public bool IsGroupByDate
			=> UserSettingsService.FoldersSettingsService.DefaultGroupOption.IsGroupByDate();

		private int selectedDefaultSortPriorityIndex;
		public int SelectedDefaultSortPriorityIndex
		{
			get => selectedDefaultSortPriorityIndex;
			set
			{
				if (SetProperty(ref selectedDefaultSortPriorityIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultSortPriorityIndex));

					switch (value)
					{
						case 0:
							UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles = false;
							UserSettingsService.FoldersSettingsService.DefaultSortFilesFirst = false;
							break;
						case 1:
							UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles = false;
							UserSettingsService.FoldersSettingsService.DefaultSortFilesFirst = true;
							break;
						case 2:
							UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles = true;
							break;
						default:
							break;
					}
				}
			}
		}

		private int selectedDefaultSortingIndex;
		public int SelectedDefaultSortingIndex
		{
			get => selectedDefaultSortingIndex;
			set
			{
				if (SetProperty(ref selectedDefaultSortingIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultSortingIndex));

					UserSettingsService.FoldersSettingsService.DefaultSortOption = value == FileTagSortingIndex ? SortOption.FileTag : (SortOption)value;
				}
			}
		}

		private int selectedDefaultGroupingIndex;
		public int SelectedDefaultGroupingIndex
		{
			get => selectedDefaultGroupingIndex;
			set
			{
				if (SetProperty(ref selectedDefaultGroupingIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultGroupingIndex));

					UserSettingsService.FoldersSettingsService.DefaultGroupOption = value == FileTagGroupingIndex ? GroupOption.FileTag : (GroupOption)value;

					// Raise an event for the grouping option toggle switches availability
					OnPropertyChanged(nameof(IsDefaultGrouped));
					OnPropertyChanged(nameof(IsGroupByDate));
				}
			}
		}
		private int selectedDefaultLayoutModeIndex;
		public int SelectedDefaultLayoutModeIndex
		{
			get => selectedDefaultLayoutModeIndex;
			set
			{
				if (SetProperty(ref selectedDefaultLayoutModeIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultLayoutModeIndex));
					DefaultLayoutMode = (FolderLayoutModes)value;
				}
			}
		}
		public bool ShowFileTagColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowFileTagColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowFileTagColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowFileTagColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowSizeColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowSizeColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowSizeColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowSizeColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowTypeColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowTypeColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowTypeColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowTypeColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowDateCreatedColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowDateColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowDateColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowDateColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowDateColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public FolderLayoutModes DefaultLayoutMode
		{
			get => UserSettingsService.LayoutSettingsService.DefaultLayoutMode;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.DefaultLayoutMode)
				{
					UserSettingsService.LayoutSettingsService.DefaultLayoutMode = value;

					OnPropertyChanged();
				}
			}
		}


		public void ResetLayoutPreferences()
		{
			// Is this proper practice?
			var dbInstance = LayoutPreferencesManager.GetDatabaseManagerInstance();

			dbInstance.ResetAll();
		}
	}
}
