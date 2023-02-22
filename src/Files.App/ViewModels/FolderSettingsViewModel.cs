using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.EventArguments;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.LayoutPreferences;
using Files.App.Views.LayoutModes;
using Files.Core.Services.Settings;
using Files.Core.Enums;
using Files.Core.Extensions;
using System;
using System.Text.Json;
using System.Windows.Input;
using Windows.Storage;
using IO = System.IO;

namespace Files.App.ViewModels
{
	public class FolderSettingsViewModel : ObservableObject
	{
		public static string LayoutSettingsDbPath
			=> IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "user_settings.db");

		private static readonly Lazy<LayoutPrefsDb> dbInstance = new(() => new LayoutPrefsDb(LayoutSettingsDbPath, true));

		public static LayoutPrefsDb GetDbInstance()
			=> dbInstance.Value;

		public event EventHandler<LayoutPreferenceEventArgs>? LayoutPreferencesUpdateRequired;

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public FolderSettingsViewModel()
		{
			LayoutPreference = new LayoutPreferences();

			ToggleLayoutModeGridViewLargeCommand = new RelayCommand<bool>(ToggleLayoutModeGridViewLarge);
			ToggleLayoutModeColumnViewCommand = new RelayCommand<bool>(ToggleLayoutModeColumnView);
			ToggleLayoutModeGridViewMediumCommand = new RelayCommand<bool>(ToggleLayoutModeGridViewMedium);
			ToggleLayoutModeGridViewSmallCommand = new RelayCommand<bool>(ToggleLayoutModeGridViewSmall);
			ToggleLayoutModeGridViewCommand = new RelayCommand<int>(ToggleLayoutModeGridView);
			ToggleLayoutModeTilesCommand = new RelayCommand<bool>(ToggleLayoutModeTiles);
			ToggleLayoutModeDetailsViewCommand = new RelayCommand<bool>(ToggleLayoutModeDetailsView);
			ToggleLayoutModeAdaptiveCommand = new RelayCommand(ToggleLayoutModeAdaptive);

			ChangeGroupOptionCommand = new RelayCommand<GroupOption>(ChangeGroupOption);
			ChangeGroupDirectionCommand = new RelayCommand<SortDirection>(ChangeGroupDirection);
		}

		public FolderSettingsViewModel(FolderLayoutModes modeOverride) : this()
			=> (rootLayoutMode, LayoutPreference.IsAdaptiveLayoutOverridden) = (modeOverride, true);

		private readonly FolderLayoutModes? rootLayoutMode;

		public bool IsLayoutModeFixed
			=> rootLayoutMode is not null;

		public bool IsAdaptiveLayoutEnabled
		{
			get => !LayoutPreference.IsAdaptiveLayoutOverridden;
			set
			{
				if (SetProperty(ref LayoutPreference.IsAdaptiveLayoutOverridden, !value, nameof(IsAdaptiveLayoutEnabled)))
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference, true));
			}
		}

		public FolderLayoutModes LayoutMode
		{
			get => rootLayoutMode ?? LayoutPreference.LayoutMode;
			set
			{
				if (SetProperty(ref LayoutPreference.LayoutMode, value, nameof(LayoutMode)))
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
			}
		}

		public uint GetIconSize()
		{
			// ListView thumbnail
			if (LayoutMode == FolderLayoutModes.DetailsView)
				return Constants.Browser.DetailsLayoutBrowser.DetailsViewSize;
			// ListView thumbnail
			else if (LayoutMode == FolderLayoutModes.ColumnView)
				return Constants.Browser.ColumnViewBrowser.ColumnViewSize;
			// Small thumbnail
			else if (LayoutMode == FolderLayoutModes.TilesView)
				return Constants.Browser.GridViewBrowser.GridViewSizeSmall;
			// Small thumbnail
			else if (GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeSmall)
				return Constants.Browser.GridViewBrowser.GridViewSizeSmall;
			// Medium thumbnail
			else if (GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeMedium)
				return Constants.Browser.GridViewBrowser.GridViewSizeMedium;
			// Large thumbnail
			else if (GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeLarge)
				return Constants.Browser.GridViewBrowser.GridViewSizeLarge;
			// Extra large thumbnail
			else
				return Constants.Browser.GridViewBrowser.GridViewSizeMax;
		}

		private bool isLayoutModeChanging;
		public bool IsLayoutModeChanging
		{
			get => isLayoutModeChanging;
			set => SetProperty(ref isLayoutModeChanging, value);
		}

		public Type GetLayoutType(string folderPath)
		{
			var prefsForPath = GetLayoutPreferencesForPath(folderPath);
			IsLayoutModeChanging = LayoutPreference.LayoutMode != prefsForPath.LayoutMode;
			LayoutPreference = prefsForPath;

			return (prefsForPath.LayoutMode) switch
			{
				FolderLayoutModes.DetailsView => typeof(DetailsLayoutBrowser),
				FolderLayoutModes.TilesView => typeof(GridViewBrowser),
				FolderLayoutModes.GridView => typeof(GridViewBrowser),
				FolderLayoutModes.ColumnView => typeof(ColumnViewBrowser),
				_ => typeof(DetailsLayoutBrowser)
			};
		}

		public event EventHandler<LayoutModeEventArgs>? LayoutModeChangeRequested;

		public event EventHandler? GridViewSizeChangeRequested;

		public ICommand ToggleLayoutModeGridViewLargeCommand { get; }
		public ICommand ToggleLayoutModeColumnViewCommand { get; }
		public ICommand ToggleLayoutModeGridViewMediumCommand { get; }
		public ICommand ToggleLayoutModeGridViewSmallCommand { get; }
		public ICommand ToggleLayoutModeGridViewCommand { get; }
		public ICommand ToggleLayoutModeTilesCommand { get; }
		public ICommand ToggleLayoutModeDetailsViewCommand { get; }
		public ICommand ToggleLayoutModeAdaptiveCommand { get; }

		public GridViewSizeKind GridViewSizeKind
		{
			get
			{
				if (GridViewSize < Constants.Browser.GridViewBrowser.GridViewSizeMedium)
					return GridViewSizeKind.Small;
				else if (GridViewSize >= Constants.Browser.GridViewBrowser.GridViewSizeMedium && GridViewSize < Constants.Browser.GridViewBrowser.GridViewSizeLarge)
					return GridViewSizeKind.Medium;
				else
					return GridViewSizeKind.Large;
			}
		}

		public int GridViewSize
		{
			get => LayoutPreference.GridViewSize;
			set
			{
				// Size down
				if (value < LayoutPreference.GridViewSize)
				{
					// Size down from tiles to list
					if (LayoutMode == FolderLayoutModes.TilesView)
					{
						LayoutPreference.IsAdaptiveLayoutOverridden = true;
						LayoutMode = FolderLayoutModes.DetailsView;
						LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
					}
					// Size down from grid to tiles
					else if (LayoutMode == FolderLayoutModes.GridView && value < Constants.Browser.GridViewBrowser.GridViewSizeSmall)
					{
						LayoutPreference.IsAdaptiveLayoutOverridden = true;
						LayoutMode = FolderLayoutModes.TilesView;
						LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
					}
					// Resize grid view
					else if (LayoutMode != FolderLayoutModes.DetailsView)
					{
						// Set grid size to allow immediate UI update
						var newValue = (value >= Constants.Browser.GridViewBrowser.GridViewSizeSmall) ? value : Constants.Browser.GridViewBrowser.GridViewSizeSmall;
						SetProperty(ref LayoutPreference.GridViewSize, newValue, nameof(GridViewSize));

						// Only update layout mode if it isn't already in grid view
						if (LayoutMode != FolderLayoutModes.GridView)
						{
							LayoutPreference.IsAdaptiveLayoutOverridden = true;
							LayoutMode = FolderLayoutModes.GridView;
							LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
						}
						else
						{
							LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
						}

						GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
					}
				}
				// Size up
				else if (value > LayoutPreference.GridViewSize)
				{
					// Size up from list to tiles
					if (LayoutMode == FolderLayoutModes.DetailsView)
					{
						LayoutPreference.IsAdaptiveLayoutOverridden = true;
						LayoutMode = FolderLayoutModes.TilesView;
						LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
					}
					else // Size up from tiles to grid
					{
						// Set grid size to allow immediate UI update
						var newValue = (LayoutMode == FolderLayoutModes.TilesView) ? Constants.Browser.GridViewBrowser.GridViewSizeSmall : (value <= Constants.Browser.GridViewBrowser.GridViewSizeMax) ? value : Constants.Browser.GridViewBrowser.GridViewSizeMax;
						SetProperty(ref LayoutPreference.GridViewSize, newValue, nameof(GridViewSize));

						// Only update layout mode if it isn't already in grid view
						if (LayoutMode != FolderLayoutModes.GridView)
						{
							LayoutPreference.IsAdaptiveLayoutOverridden = true;
							LayoutMode = FolderLayoutModes.GridView;
							LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
						}
						else
						{
							LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
						}

						// Don't request a grid resize if it is already at the max size
						if (value < Constants.Browser.GridViewBrowser.GridViewSizeMax)
							GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
					}
				}
			}
		}

		public event EventHandler<SortOption>? SortOptionPreferenceUpdated;

		public event EventHandler<GroupOption>? GroupOptionPreferenceUpdated;

		public event EventHandler<SortDirection>? SortDirectionPreferenceUpdated;

		public event EventHandler<SortDirection>? GroupDirectionPreferenceUpdated;

		public event EventHandler<bool>? SortDirectoriesAlongsideFilesPreferenceUpdated;

		public SortOption DirectorySortOption
		{
			get => LayoutPreference.DirectorySortOption;
			set
			{
				if (SetProperty(ref LayoutPreference.DirectorySortOption, value, nameof(DirectorySortOption)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
					SortOptionPreferenceUpdated?.Invoke(this, DirectorySortOption);
				}
			}
		}

		public ICommand ChangeGroupOptionCommand { get; }

		public GroupOption DirectoryGroupOption
		{
			get => LayoutPreference.DirectoryGroupOption;
			set
			{
				if (SetProperty(ref LayoutPreference.DirectoryGroupOption, value, nameof(DirectoryGroupOption)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
					GroupOptionPreferenceUpdated?.Invoke(this, DirectoryGroupOption);
				}
			}
		}

		public SortDirection DirectorySortDirection
		{
			get => LayoutPreference.DirectorySortDirection;
			set
			{
				if (SetProperty(ref LayoutPreference.DirectorySortDirection, value, nameof(DirectorySortDirection)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
					SortDirectionPreferenceUpdated?.Invoke(this, DirectorySortDirection);
				}
			}
		}

		public ICommand ChangeGroupDirectionCommand { get; }

		public SortDirection DirectoryGroupDirection
		{
			get => LayoutPreference.DirectoryGroupDirection;
			set
			{
				if (SetProperty(ref LayoutPreference.DirectoryGroupDirection, value, nameof(DirectoryGroupDirection)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
					GroupDirectionPreferenceUpdated?.Invoke(this, DirectoryGroupDirection);
				}
			}
		}

		public bool SortDirectoriesAlongsideFiles
		{
			get
			{
				return LayoutPreference.SortDirectoriesAlongsideFiles;
			}
			set
			{
				if (SetProperty(ref LayoutPreference.SortDirectoriesAlongsideFiles, value, nameof(SortDirectoriesAlongsideFiles)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
					SortDirectoriesAlongsideFilesPreferenceUpdated?.Invoke(this, SortDirectoriesAlongsideFiles);
				}
			}
		}

		public ColumnsViewModel ColumnsViewModel
		{
			get => LayoutPreference.ColumnsViewModel;
			set
			{
				SetProperty(ref LayoutPreference.ColumnsViewModel, value, nameof(ColumnsViewModel));
				LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
			}
		}

		private static LayoutPreferences GetLayoutPreferencesForPath(string folderPath)
		{
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			if (!userSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories)
			{
				folderPath = folderPath.TrimPath();
				var folderFRN = NativeFileOperationsHelper.GetFolderFRN(folderPath);
				return ReadLayoutPreferencesFromDb(folderPath, folderFRN)
					?? ReadLayoutPreferencesFromAds(folderPath, folderFRN)
					?? GetDefaultLayoutPreferences(folderPath);
			}

			return LayoutPreferences.DefaultLayoutPreferences;
		}

		public static void SetLayoutPreferencesForPath(string folderPath, LayoutPreferences prefs)
		{
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			if (!userSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories)
			{
				var folderFRN = NativeFileOperationsHelper.GetFolderFRN(folderPath);
				WriteLayoutPreferencesToDb(folderPath.TrimPath(), folderFRN, prefs);
			}
			else
			{
				userSettingsService.FoldersSettingsService.DefaultLayoutMode = prefs.LayoutMode;
				userSettingsService.LayoutSettingsService.DefaultGridViewSize = prefs.GridViewSize;

				// Do not save options which only work in recycle bin or cloud folders as global
				if (prefs.DirectorySortOption != SortOption.OriginalFolder &&
					prefs.DirectorySortOption != SortOption.DateDeleted &&
					prefs.DirectorySortOption != SortOption.SyncStatus)
				{
					userSettingsService.FoldersSettingsService.DefaultSortOption = prefs.DirectorySortOption;
				}

				if (prefs.DirectoryGroupOption != GroupOption.OriginalFolder &&
					prefs.DirectoryGroupOption != GroupOption.DateDeleted &&
					prefs.DirectoryGroupOption != GroupOption.FolderPath &&
					prefs.DirectoryGroupOption != GroupOption.SyncStatus)
				{
					userSettingsService.FoldersSettingsService.DefaultGroupOption = prefs.DirectoryGroupOption;
				}

				userSettingsService.FoldersSettingsService.DefaultDirectorySortDirection = prefs.DirectorySortDirection;
				userSettingsService.FoldersSettingsService.DefaultDirectoryGroupDirection = prefs.DirectoryGroupDirection;
				userSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles = prefs.SortDirectoriesAlongsideFiles;

				userSettingsService.FoldersSettingsService.ShowDateColumn = !prefs.ColumnsViewModel.DateModifiedColumn.UserCollapsed;
				userSettingsService.FoldersSettingsService.ShowDateCreatedColumn = !prefs.ColumnsViewModel.DateCreatedColumn.UserCollapsed;
				userSettingsService.FoldersSettingsService.ShowTypeColumn = !prefs.ColumnsViewModel.ItemTypeColumn.UserCollapsed;
				userSettingsService.FoldersSettingsService.ShowSizeColumn = !prefs.ColumnsViewModel.SizeColumn.UserCollapsed;
				userSettingsService.FoldersSettingsService.ShowFileTagColumn = !prefs.ColumnsViewModel.TagColumn.UserCollapsed;
				userSettingsService.FoldersSettingsService.ShowDateDeletedColumn = !prefs.ColumnsViewModel.DateDeletedColumn.UserCollapsed;
				userSettingsService.FoldersSettingsService.ShowOriginalPathColumn = !prefs.ColumnsViewModel.OriginalPathColumn.UserCollapsed;
				userSettingsService.FoldersSettingsService.ShowSyncStatusColumn = !prefs.ColumnsViewModel.StatusColumn.UserCollapsed;

				userSettingsService.FoldersSettingsService.NameColumnWidth = prefs.ColumnsViewModel.NameColumn.UserLengthPixels;
				userSettingsService.FoldersSettingsService.DateModifiedColumnWidth = prefs.ColumnsViewModel.DateModifiedColumn.UserLengthPixels;
				userSettingsService.FoldersSettingsService.DateCreatedColumnWidth = prefs.ColumnsViewModel.DateCreatedColumn.UserLengthPixels;
				userSettingsService.FoldersSettingsService.TypeColumnWidth = prefs.ColumnsViewModel.ItemTypeColumn.UserLengthPixels;
				userSettingsService.FoldersSettingsService.SizeColumnWidth = prefs.ColumnsViewModel.SizeColumn.UserLengthPixels;
				userSettingsService.FoldersSettingsService.TagColumnWidth = prefs.ColumnsViewModel.TagColumn.UserLengthPixels;
				userSettingsService.FoldersSettingsService.DateDeletedColumnWidth = prefs.ColumnsViewModel.DateDeletedColumn.UserLengthPixels;
				userSettingsService.FoldersSettingsService.OriginalPathColumnWidth = prefs.ColumnsViewModel.OriginalPathColumn.UserLengthPixels;
				userSettingsService.FoldersSettingsService.SyncStatusColumnWidth = prefs.ColumnsViewModel.StatusColumn.UserLengthPixels;
			}
		}

		private static LayoutPreferences ReadLayoutPreferencesFromAds(string folderPath, ulong? frn)
		{
			var str = NativeFileOperationsHelper.ReadStringFromFile($"{folderPath}:files_layoutmode");

			var adsPrefs = SafetyExtensions.IgnoreExceptions(() =>
				string.IsNullOrEmpty(str) ? null : JsonSerializer.Deserialize<LayoutPreferences>(str));

			// Port settings to DB, delete ADS
			WriteLayoutPreferencesToDb(folderPath, frn, adsPrefs);
			NativeFileOperationsHelper.DeleteFileFromApp($"{folderPath}:files_layoutmode");

			return adsPrefs;
		}

		private static LayoutPreferences? ReadLayoutPreferencesFromDb(string folderPath, ulong? frn)
		{
			if (string.IsNullOrEmpty(folderPath))
				return null;

			var dbInstance = GetDbInstance();

			return dbInstance.GetPreferences(folderPath, frn);
		}

		private static LayoutPreferences GetDefaultLayoutPreferences(string folderPath)
		{
			if (string.IsNullOrEmpty(folderPath))
				return LayoutPreferences.DefaultLayoutPreferences;

			if (folderPath == CommonPaths.DownloadsPath)
				// Default for downloads folder is to group by date created
				return new LayoutPreferences() {
					DirectoryGroupOption = GroupOption.DateCreated,
					DirectoryGroupDirection = SortDirection.Descending
				};
			else if (LibraryManager.IsLibraryPath(folderPath))
				// Default for libraries is to group by folder path
				return new LayoutPreferences() { DirectoryGroupOption = GroupOption.FolderPath };
			else
				// Either global setting or smart guess
				return LayoutPreferences.DefaultLayoutPreferences;
		}

		private static void WriteLayoutPreferencesToDb(string folderPath, ulong? frn, LayoutPreferences prefs)
		{
			if (string.IsNullOrEmpty(folderPath))
				return;

			var dbInstance = GetDbInstance();
			if (dbInstance.GetPreferences(folderPath, frn) is null &&
				LayoutPreferences.DefaultLayoutPreferences.Equals(prefs))
			{
				// Do not create setting if it's default
				return;
			}

			dbInstance.SetPreferences(folderPath, frn, prefs);
		}

		private LayoutPreferences layoutPreference;

		public LayoutPreferences LayoutPreference
		{
			get => layoutPreference;
			private set
			{
				if (SetProperty(ref layoutPreference, value))
				{
					OnPropertyChanged(nameof(LayoutMode));
					OnPropertyChanged(nameof(GridViewSize));
					OnPropertyChanged(nameof(GridViewSizeKind));
					OnPropertyChanged(nameof(IsAdaptiveLayoutEnabled));
					OnPropertyChanged(nameof(DirectoryGroupOption));
					OnPropertyChanged(nameof(DirectorySortOption));
					OnPropertyChanged(nameof(DirectorySortDirection));
					OnPropertyChanged(nameof(DirectoryGroupDirection));
					OnPropertyChanged(nameof(SortDirectoriesAlongsideFiles));
					OnPropertyChanged(nameof(ColumnsViewModel));
				}
			}
		}

		public void ToggleLayoutModeGridViewLarge(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Grid View
			LayoutMode = FolderLayoutModes.GridView;

			// Size
			GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeLarge;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.GridView, GridViewSize));
		}

		public void ToggleLayoutModeColumnView(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Column View
			LayoutMode = FolderLayoutModes.ColumnView;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.ColumnView, GridViewSize));
		}

		public void ToggleLayoutModeGridViewMedium(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Grid View
			LayoutMode = FolderLayoutModes.GridView;

			// Size
			GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeMedium;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.GridView, GridViewSize));
		}

		public void ToggleLayoutModeGridViewSmall(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Grid View
			LayoutMode = FolderLayoutModes.GridView;

			// Size
			GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeSmall;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.GridView, GridViewSize));
		}

		public void ToggleLayoutModeGridView(int size)
		{
			// Grid View
			LayoutMode = FolderLayoutModes.GridView;

			// Size
			GridViewSize = size;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
		}

		public void ToggleLayoutModeTiles(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Tiles View
			LayoutMode = FolderLayoutModes.TilesView;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.TilesView, GridViewSize));
		}

		public void ToggleLayoutModeDetailsView(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Details View
			LayoutMode = FolderLayoutModes.DetailsView;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.DetailsView, GridViewSize));
		}

		public void ToggleLayoutModeAdaptive()
		{
			// Adaptive
			IsAdaptiveLayoutEnabled = true;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.Adaptive, GridViewSize));
		}

		private void ChangeGroupOption(GroupOption option)
			=> DirectoryGroupOption = option;

		private void ChangeGroupDirection(SortDirection option) => DirectoryGroupDirection = option;

		public void OnDefaultPreferencesChanged(string folderPath, string settingsName)
		{
			var prefs = GetLayoutPreferencesForPath(folderPath);

			switch (settingsName)
			{
				case nameof(UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles):
					SortDirectoriesAlongsideFiles = prefs.SortDirectoriesAlongsideFiles;
					break;
				case nameof(UserSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories):
					LayoutPreference = prefs;
					// TODO: Update layout
					break;
			}
		}

		public void SetDefaultLayoutPreferences(ColumnsViewModel columns)
		{
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			userSettingsService.FoldersSettingsService.ShowDateColumn = !columns.DateModifiedColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowDateCreatedColumn = !columns.DateCreatedColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowTypeColumn = !columns.ItemTypeColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowSizeColumn = !columns.SizeColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowFileTagColumn = !columns.TagColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowDateDeletedColumn = !columns.DateDeletedColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowOriginalPathColumn = !columns.OriginalPathColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowSyncStatusColumn = !columns.StatusColumn.UserCollapsed;

			userSettingsService.FoldersSettingsService.NameColumnWidth = columns.NameColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.DateModifiedColumnWidth = columns.DateModifiedColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.DateCreatedColumnWidth = columns.DateCreatedColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.TypeColumnWidth = columns.ItemTypeColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.SizeColumnWidth = columns.SizeColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.TagColumnWidth = columns.TagColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.DateDeletedColumnWidth = columns.DateDeletedColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.OriginalPathColumnWidth = columns.OriginalPathColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.SyncStatusColumnWidth = columns.StatusColumn.UserLengthPixels;
		}
	}
}
