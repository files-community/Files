using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;

namespace Files.App.ViewModels.SettingsViewModels
{
	public class FoldersViewModel : ObservableObject
	{
		private readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();


		//FileTag combobox indexes (required to hide SyncStatus)
		private readonly int FileTagSortingIndex = 5;
		private readonly int FileTagGroupingIndex = 6;

		// Commands
		public RelayCommand ResetLayoutPreferencesCommand { get; }
		public RelayCommand ShowResetLayoutPreferencesTipCommand { get; }

		public FoldersViewModel()
		{
			ResetLayoutPreferencesCommand = new RelayCommand(ResetLayoutPreferences);
			ShowResetLayoutPreferencesTipCommand = new RelayCommand(() => IsResetLayoutPreferencesTipOpen = true);

			SelectedDefaultLayoutModeIndex = (int)DefaultLayoutMode;
			SelectedDefaultSortingIndex = userSettingsService.FoldersSettingsService.DefaultSortOption == SortOption.FileTag ? FileTagSortingIndex : (int)userSettingsService.FoldersSettingsService.DefaultSortOption;
			SelectedDefaultGroupingIndex = userSettingsService.FoldersSettingsService.DefaultGroupOption == GroupOption.FileTag ? FileTagGroupingIndex : (int)userSettingsService.FoldersSettingsService.DefaultGroupOption;
		}

		// Properties

		private bool isResetLayoutPreferencesTipOpen;
		public bool IsResetLayoutPreferencesTipOpen
		{
			get => isResetLayoutPreferencesTipOpen;
			set => SetProperty(ref isResetLayoutPreferencesTipOpen, value);
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

		public bool EnableOverridingFolderPreferences
		{
			get => userSettingsService.FoldersSettingsService.EnableOverridingFolderPreferences;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.EnableOverridingFolderPreferences)
				{
					userSettingsService.FoldersSettingsService.EnableOverridingFolderPreferences = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowFileTagColumn
		{
			get => userSettingsService.FoldersSettingsService.ShowFileTagColumn;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.ShowFileTagColumn)
				{
					userSettingsService.FoldersSettingsService.ShowFileTagColumn = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowSizeColumn
		{
			get => userSettingsService.FoldersSettingsService.ShowSizeColumn;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.ShowSizeColumn)
				{
					userSettingsService.FoldersSettingsService.ShowSizeColumn = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowTypeColumn
		{
			get => userSettingsService.FoldersSettingsService.ShowTypeColumn;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.ShowTypeColumn)
				{
					userSettingsService.FoldersSettingsService.ShowTypeColumn = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowDateCreatedColumn
		{
			get => userSettingsService.FoldersSettingsService.ShowDateCreatedColumn;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.ShowDateCreatedColumn)
				{
					userSettingsService.FoldersSettingsService.ShowDateCreatedColumn = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowDateColumn
		{
			get => userSettingsService.FoldersSettingsService.ShowDateColumn;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.ShowDateColumn)
				{
					userSettingsService.FoldersSettingsService.ShowDateColumn = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowSelectionCheckboxes
		{
			get => UserSettingsService.FoldersSettingsService.ShowSelectionCheckboxes;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowSelectionCheckboxes)
				{
					UserSettingsService.FoldersSettingsService.ShowSelectionCheckboxes = value;
					OnPropertyChanged();
				}
			}
		}

		public FolderLayoutModes DefaultLayoutMode
		{
			get => userSettingsService.FoldersSettingsService.DefaultLayoutMode;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.DefaultLayoutMode)
				{
					userSettingsService.FoldersSettingsService.DefaultLayoutMode = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowHiddenItems
		{
			get => userSettingsService.FoldersSettingsService.ShowHiddenItems;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.ShowHiddenItems)
				{
					userSettingsService.FoldersSettingsService.ShowHiddenItems = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowProtectedSystemFiles
		{
			get => userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles)
				{
					userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles = value;
					OnPropertyChanged();
				}
			}
		}

		public bool AreAlternateStreamsVisible
		{
			get => userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible)
				{
					userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowDotFiles
		{
			get => userSettingsService.FoldersSettingsService.ShowDotFiles;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.ShowDotFiles)
				{
					userSettingsService.FoldersSettingsService.ShowDotFiles = value;
					OnPropertyChanged();
				}
			}
		}

		public bool OpenItemsWithOneClick
		{
			get => userSettingsService.FoldersSettingsService.OpenItemsWithOneClick;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
				{
					userSettingsService.FoldersSettingsService.OpenItemsWithOneClick = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ColumnLayoutOpenFoldersWithOneClick
		{
			get => userSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick)
				{
					userSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick = value;
					OnPropertyChanged();
				}
			}
		}

		public bool OpenFoldersNewTab
		{
			get => userSettingsService.FoldersSettingsService.OpenFoldersInNewTab;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.OpenFoldersInNewTab)
				{
					userSettingsService.FoldersSettingsService.OpenFoldersInNewTab = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ListAndSortDirectoriesAlongsideFiles
		{
			get => userSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles;
			set
			{
				if (value != userSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles)
				{
					userSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles = value;
					OnPropertyChanged();
				}
			}
		}

		public bool CalculateFolderSizes
		{
			get => userSettingsService.FoldersSettingsService.CalculateFolderSizes;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.CalculateFolderSizes)
				{
					userSettingsService.FoldersSettingsService.CalculateFolderSizes = value;
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
					userSettingsService.FoldersSettingsService.DefaultSortOption = value == FileTagSortingIndex ? SortOption.FileTag : (SortOption)value;
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
					userSettingsService.FoldersSettingsService.DefaultGroupOption = value == FileTagGroupingIndex ? GroupOption.FileTag : (GroupOption)value;
				}
			}
		}

		public bool ShowFileExtensions
		{
			get => userSettingsService.FoldersSettingsService.ShowFileExtensions;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.ShowFileExtensions)
				{
					userSettingsService.FoldersSettingsService.ShowFileExtensions = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowThumbnails
		{
			get => userSettingsService.FoldersSettingsService.ShowThumbnails;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.ShowThumbnails)
				{
					userSettingsService.FoldersSettingsService.ShowThumbnails = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowConfirmDeleteDialog
		{
			get => userSettingsService.FoldersSettingsService.ShowConfirmDeleteDialog;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.ShowConfirmDeleteDialog)
				{
					userSettingsService.FoldersSettingsService.ShowConfirmDeleteDialog = value;
					OnPropertyChanged();
				}
			}
		}

		public bool SelectFilesOnHover
		{
			get => userSettingsService.FoldersSettingsService.SelectFilesOnHover;
			set
			{
				if (value != userSettingsService.FoldersSettingsService.SelectFilesOnHover)
				{
					userSettingsService.FoldersSettingsService.SelectFilesOnHover = value;
					OnPropertyChanged();
				}
			}
		}

		// Local methods

		public void ResetLayoutPreferences()
		{
			// Is this proper practice?
			var dbInstance = FolderSettingsViewModel.GetDbInstance();
			dbInstance.ResetAll();
			IsResetLayoutPreferencesTipOpen = false;
		}
	}
}