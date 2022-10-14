using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;

namespace Files.App.ViewModels.SettingsViewModels
{
	public class FoldersViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Commands
		public RelayCommand ResetLayoutPreferencesCommand { get; }
		public RelayCommand ShowResetLayoutPreferencesTipCommand { get; }



		public FoldersViewModel()
		{
			ResetLayoutPreferencesCommand = new RelayCommand(ResetLayoutPreferences);
			ShowResetLayoutPreferencesTipCommand = new RelayCommand(() => IsResetLayoutPreferencesTipOpen = true);

			SelectedDefaultLayoutModeIndex = (long)DefaultLayoutMode;
		}

		// Properties

		private bool isResetLayoutPreferencesTipOpen;
		public bool IsResetLayoutPreferencesTipOpen
		{
			get => isResetLayoutPreferencesTipOpen;
			set => SetProperty(ref isResetLayoutPreferencesTipOpen, value);
		}

		private long selectedDefaultLayoutModeIndex;
		public long SelectedDefaultLayoutModeIndex
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
			get => UserSettingsService.FoldersSettingsService.EnableOverridingFolderPreferences;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.EnableOverridingFolderPreferences)
				{
					UserSettingsService.FoldersSettingsService.EnableOverridingFolderPreferences = value;
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


		// Local methods

		public void ResetLayoutPreferences()
		{
			// Is this proper practice?
			FolderSettingsViewModel.DbInstance.ResetAll();
			IsResetLayoutPreferencesTipOpen = false;
		}
	}
}