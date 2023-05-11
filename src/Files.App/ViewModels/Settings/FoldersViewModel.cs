// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.Settings
{
	public class FoldersViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// FileTag combobox indexes (required to hide SyncStatus)
		private readonly int FileTagSortingIndex = 5;
		private readonly int FileTagGroupingIndex = 6;

		public FoldersViewModel()
		{
			SelectedDefaultLayoutModeIndex = (int)DefaultLayoutMode;
			SelectedDefaultSortingIndex = UserSettingsService.FoldersSettingsService.DefaultSortOption == SortOption.FileTag ? FileTagSortingIndex : (int)UserSettingsService.FoldersSettingsService.DefaultSortOption;
			SelectedDefaultGroupingIndex = UserSettingsService.FoldersSettingsService.DefaultGroupOption == GroupOption.FileTag ? FileTagGroupingIndex : (int)UserSettingsService.FoldersSettingsService.DefaultGroupOption;
			SelectedDeleteConfirmationPolicyIndex = (int)DeleteConfirmationPolicy;
		}

		// Properties

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

		private int selectedDeleteConfirmationPolicyIndex;
		public int SelectedDeleteConfirmationPolicyIndex
		{
			get => selectedDeleteConfirmationPolicyIndex;
			set
			{
				if (SetProperty(ref selectedDeleteConfirmationPolicyIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDeleteConfirmationPolicyIndex));
					DeleteConfirmationPolicy = (DeleteConfirmationPolicies)value;
				}
			}
		}

		public bool SyncFolderPreferencesAcrossDirectories
		{
			get => UserSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories)
				{
					UserSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories = value;

					ResetLayoutPreferences();
					OnPropertyChanged();
				}
			}
		}

		public bool ShowFileTagColumn
		{
			get => UserSettingsService.FoldersSettingsService.ShowFileTagColumn;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowFileTagColumn)
				{
					UserSettingsService.FoldersSettingsService.ShowFileTagColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowSizeColumn
		{
			get => UserSettingsService.FoldersSettingsService.ShowSizeColumn;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowSizeColumn)
				{
					UserSettingsService.FoldersSettingsService.ShowSizeColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowTypeColumn
		{
			get => UserSettingsService.FoldersSettingsService.ShowTypeColumn;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowTypeColumn)
				{
					UserSettingsService.FoldersSettingsService.ShowTypeColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowDateCreatedColumn
		{
			get => UserSettingsService.FoldersSettingsService.ShowDateCreatedColumn;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowDateCreatedColumn)
				{
					UserSettingsService.FoldersSettingsService.ShowDateCreatedColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowDateColumn
		{
			get => UserSettingsService.FoldersSettingsService.ShowDateColumn;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowDateColumn)
				{
					UserSettingsService.FoldersSettingsService.ShowDateColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public FolderLayoutModes DefaultLayoutMode
		{
			get => UserSettingsService.FoldersSettingsService.DefaultLayoutMode;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.DefaultLayoutMode)
				{
					UserSettingsService.FoldersSettingsService.DefaultLayoutMode = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowHiddenItems
		{
			get => UserSettingsService.FoldersSettingsService.ShowHiddenItems;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowHiddenItems)
				{
					UserSettingsService.FoldersSettingsService.ShowHiddenItems = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowProtectedSystemFiles
		{
			get => UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles)
				{
					UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles = value;

					OnPropertyChanged();
				}
			}
		}

		public bool AreAlternateStreamsVisible
		{
			get => UserSettingsService.FoldersSettingsService.AreAlternateStreamsVisible;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.AreAlternateStreamsVisible)
				{
					UserSettingsService.FoldersSettingsService.AreAlternateStreamsVisible = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowDotFiles
		{
			get => UserSettingsService.FoldersSettingsService.ShowDotFiles;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowDotFiles)
				{
					UserSettingsService.FoldersSettingsService.ShowDotFiles = value;

					OnPropertyChanged();
				}
			}
		}

		public bool OpenItemsWithOneClick
		{
			get => UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
				{
					UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ColumnLayoutOpenFoldersWithOneClick
		{
			get => UserSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick)
				{
					UserSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick = value;

					OnPropertyChanged();
				}
			}
		}

		public bool OpenFoldersNewTab
		{
			get => UserSettingsService.FoldersSettingsService.OpenFoldersInNewTab;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.OpenFoldersInNewTab)
				{
					UserSettingsService.FoldersSettingsService.OpenFoldersInNewTab = value;

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

		public bool GroupByMonth
		{
			get => UserSettingsService.FoldersSettingsService.DefaultGroupByDateUnit == GroupByDateUnit.Month;
			set
			{
				if (value != (UserSettingsService.FoldersSettingsService.DefaultGroupByDateUnit == GroupByDateUnit.Month))
				{
					UserSettingsService.FoldersSettingsService.DefaultGroupByDateUnit = value ? GroupByDateUnit.Month : GroupByDateUnit.Year;
					OnPropertyChanged();
				}
			}
		}

		public bool IsGroupByDate
			=> UserSettingsService.FoldersSettingsService.DefaultGroupOption.IsGroupByDate();

		public bool ListAndSortDirectoriesAlongsideFiles
		{
			get => UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles)
				{
					UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles = value;

					OnPropertyChanged();
				}
			}
		}

		public bool CalculateFolderSizes
		{
			get => UserSettingsService.FoldersSettingsService.CalculateFolderSizes;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.CalculateFolderSizes)
				{
					UserSettingsService.FoldersSettingsService.CalculateFolderSizes = value;

					OnPropertyChanged();
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

		public bool ShowFileExtensions
		{
			get => UserSettingsService.FoldersSettingsService.ShowFileExtensions;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowFileExtensions)
				{
					UserSettingsService.FoldersSettingsService.ShowFileExtensions = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowThumbnails
		{
			get => UserSettingsService.FoldersSettingsService.ShowThumbnails;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowThumbnails)
				{
					UserSettingsService.FoldersSettingsService.ShowThumbnails = value;

					OnPropertyChanged();
				}
			}
		}

		public DeleteConfirmationPolicies DeleteConfirmationPolicy
		{
			get => UserSettingsService.FoldersSettingsService.DeleteConfirmationPolicy;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.DeleteConfirmationPolicy)
				{
					UserSettingsService.FoldersSettingsService.DeleteConfirmationPolicy = value;

					OnPropertyChanged();
				}
			}
		}

		public bool SelectFilesOnHover
		{
			get => UserSettingsService.FoldersSettingsService.SelectFilesOnHover;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.SelectFilesOnHover)
				{
					UserSettingsService.FoldersSettingsService.SelectFilesOnHover = value;

					OnPropertyChanged();
				}
			}
		}

		public bool DoubleClickToGoUp
		{
			get => UserSettingsService.FoldersSettingsService.DoubleClickToGoUp;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
				{
					UserSettingsService.FoldersSettingsService.DoubleClickToGoUp = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowFileExtensionWarning
		{
			get => UserSettingsService.FoldersSettingsService.ShowFileExtensionWarning;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowFileExtensionWarning)
				{
					UserSettingsService.FoldersSettingsService.ShowFileExtensionWarning = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowCheckboxesWhenSelectingItems
		{
			get => UserSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems)
				{
					UserSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems = value;

					OnPropertyChanged();
				}
			}
		}

		public void ResetLayoutPreferences()
		{
			// Is this proper practice?
			var dbInstance = FolderSettingsViewModel.GetDbInstance();

			dbInstance.ResetAll();
		}
	}
}
