// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Text.Json;
using Windows.Storage;

namespace Files.App.Data.Models
{
	/// <summary>
	/// Represents manager for layout settings.
	/// Provides richer functions than <see cref="LayoutPreferencesManager"/>.
	/// </summary>
	public class LayoutSettingsManager : ObservableObject
	{
		// Dependency injections

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Fields

		private static readonly Lazy<LayoutPreferencesDatabase> _databaseInstance = new(() => new LayoutPreferencesDatabase(LayoutSettingsDbPath, true));

		private readonly FolderLayoutModes? _rootLayoutMode;

		// Properties

		public static string LayoutSettingsDbPath
			=> SystemIO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "user_settings.db");

		public bool IsLayoutModeFixed
			=> _rootLayoutMode is not null;

		public bool IsAdaptiveLayoutEnabled
		{
			get => !LayoutPreference.IsAdaptiveLayoutOverridden;
			set
			{
				if (SetProperty(ref LayoutPreference.IsAdaptiveLayoutOverridden, !value, nameof(IsAdaptiveLayoutEnabled)))
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference, true));
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

		public FolderLayoutModes LayoutMode
		{
			get => _rootLayoutMode ?? LayoutPreference.LayoutMode;
			set
			{
				if (SetProperty(ref LayoutPreference.LayoutMode, value, nameof(LayoutMode)))
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
			}
		}

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

		public GroupByDateUnit DirectoryGroupByDateUnit
		{
			get => LayoutPreference.DirectoryGroupByDateUnit;
			set
			{
				if (SetProperty(ref LayoutPreference.DirectoryGroupByDateUnit, value, nameof(DirectoryGroupByDateUnit)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
					GroupByDateUnitPreferenceUpdated?.Invoke(this, DirectoryGroupByDateUnit);
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

		private bool isLayoutModeChanging;
		public bool IsLayoutModeChanging
		{
			get => isLayoutModeChanging;
			set => SetProperty(ref isLayoutModeChanging, value);
		}

		private LayoutPreferencesManager layoutPreference;
		public LayoutPreferencesManager LayoutPreference
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
					OnPropertyChanged(nameof(DirectoryGroupByDateUnit));
					OnPropertyChanged(nameof(SortDirectoriesAlongsideFiles));
					OnPropertyChanged(nameof(ColumnsViewModel));
				}
			}
		}

		// Events

		public event EventHandler<LayoutPreferenceEventArgs>? LayoutPreferencesUpdateRequired;
		public event EventHandler<SortOption>? SortOptionPreferenceUpdated;
		public event EventHandler<GroupOption>? GroupOptionPreferenceUpdated;
		public event EventHandler<SortDirection>? SortDirectionPreferenceUpdated;
		public event EventHandler<SortDirection>? GroupDirectionPreferenceUpdated;
		public event EventHandler<GroupByDateUnit>? GroupByDateUnitPreferenceUpdated;
		public event EventHandler<bool>? SortDirectoriesAlongsideFilesPreferenceUpdated;
		public event EventHandler<LayoutModeEventArgs>? LayoutModeChangeRequested;
		public event EventHandler? GridViewSizeChangeRequested;

		// Constructors

		public LayoutSettingsManager()
		{
			LayoutPreference = new LayoutPreferencesManager();
		}

		public LayoutSettingsManager(FolderLayoutModes modeOverride) : this()
		{
			_rootLayoutMode = modeOverride;
			LayoutPreference.IsAdaptiveLayoutOverridden = true;
		}

		// Methods

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

		public Type GetLayoutType(string folderPath, bool changeLayoutMode = true)
		{
			var prefsForPath = GetLayoutPreferencesForPath(folderPath);
			if (changeLayoutMode)
			{
				IsLayoutModeChanging = LayoutPreference.LayoutMode != prefsForPath.LayoutMode;
				LayoutPreference = prefsForPath;
			}

			return (prefsForPath.LayoutMode) switch
			{
				FolderLayoutModes.DetailsView => typeof(DetailsLayoutPage),
				FolderLayoutModes.TilesView => typeof(GridLayoutPage),
				FolderLayoutModes.GridView => typeof(GridLayoutPage),
				FolderLayoutModes.ColumnView => typeof(ColumnsLayoutPage),
				_ => typeof(DetailsLayoutPage)
			};
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

		public void OnDefaultPreferencesChanged(string folderPath, string settingsName)
		{
			var preferences = GetLayoutPreferencesForPath(folderPath);

			switch (settingsName)
			{
				case nameof(UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles):
					SortDirectoriesAlongsideFiles = preferences.SortDirectoriesAlongsideFiles;
					break;
				case nameof(UserSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories):
					LayoutPreference = preferences;
					// TODO: Update layout
					break;
			}
		}

		// Static methods

		public static LayoutPreferencesDatabase GetDbInstance()
		{
			return _databaseInstance.Value;
		}

		private static LayoutPreferencesManager GetLayoutPreferencesForPath(string folderPath)
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

			return LayoutPreferencesManager.DefaultLayoutPreferences;
		}

		public static void SetLayoutPreferencesForPath(string folderPath, LayoutPreferencesManager preferencesManager)
		{
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			if (!userSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories)
			{
				var folderFRN = NativeFileOperationsHelper.GetFolderFRN(folderPath);
				var trimmedFolderPath = folderPath.TrimPath();
				if (trimmedFolderPath is not null)
					WriteLayoutPreferencesToDb(trimmedFolderPath, folderFRN, preferencesManager);
			}
			else
			{
				userSettingsService.FoldersSettingsService.DefaultLayoutMode = preferencesManager.LayoutMode;
				userSettingsService.LayoutSettingsService.DefaultGridViewSize = preferencesManager.GridViewSize;

				// Do not save options which only work in recycle bin or cloud folders or search results as global
				if (preferencesManager.DirectorySortOption != SortOption.Path &&
					preferencesManager.DirectorySortOption != SortOption.OriginalFolder &&
					preferencesManager.DirectorySortOption != SortOption.DateDeleted &&
					preferencesManager.DirectorySortOption != SortOption.SyncStatus)
				{
					userSettingsService.FoldersSettingsService.DefaultSortOption = preferencesManager.DirectorySortOption;
				}

				if (preferencesManager.DirectoryGroupOption != GroupOption.OriginalFolder &&
					preferencesManager.DirectoryGroupOption != GroupOption.DateDeleted &&
					preferencesManager.DirectoryGroupOption != GroupOption.FolderPath &&
					preferencesManager.DirectoryGroupOption != GroupOption.SyncStatus)
				{
					userSettingsService.FoldersSettingsService.DefaultGroupOption = preferencesManager.DirectoryGroupOption;
				}

				userSettingsService.FoldersSettingsService.DefaultDirectorySortDirection = preferencesManager.DirectorySortDirection;
				userSettingsService.FoldersSettingsService.DefaultDirectoryGroupDirection = preferencesManager.DirectoryGroupDirection;
				userSettingsService.FoldersSettingsService.DefaultGroupByDateUnit = preferencesManager.DirectoryGroupByDateUnit;
				userSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles = preferencesManager.SortDirectoriesAlongsideFiles;

				userSettingsService.FoldersSettingsService.NameColumnWidth = preferencesManager.ColumnsViewModel.NameColumn.UserLengthPixels;
				if (!preferencesManager.ColumnsViewModel.DateModifiedColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowDateColumn = !preferencesManager.ColumnsViewModel.DateModifiedColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.DateModifiedColumnWidth = preferencesManager.ColumnsViewModel.DateModifiedColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.DateCreatedColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowDateCreatedColumn = !preferencesManager.ColumnsViewModel.DateCreatedColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.DateCreatedColumnWidth = preferencesManager.ColumnsViewModel.DateCreatedColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.ItemTypeColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowTypeColumn = !preferencesManager.ColumnsViewModel.ItemTypeColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.TypeColumnWidth = preferencesManager.ColumnsViewModel.ItemTypeColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.SizeColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowSizeColumn = !preferencesManager.ColumnsViewModel.SizeColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.SizeColumnWidth = preferencesManager.ColumnsViewModel.SizeColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.TagColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowFileTagColumn = !preferencesManager.ColumnsViewModel.TagColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.TagColumnWidth = preferencesManager.ColumnsViewModel.TagColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.GitStatusColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowGitStatusColumn = !preferencesManager.ColumnsViewModel.GitStatusColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.GitStatusColumnWidth = preferencesManager.ColumnsViewModel.GitStatusColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.GitLastCommitDateColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowGitLastCommitDateColumn = !preferencesManager.ColumnsViewModel.GitLastCommitDateColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.GitLastCommitDateColumnWidth = preferencesManager.ColumnsViewModel.GitLastCommitDateColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.GitLastCommitMessageColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowGitLastCommitMessageColumn = !preferencesManager.ColumnsViewModel.GitLastCommitMessageColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.GitLastCommitMessageColumnWidth = preferencesManager.ColumnsViewModel.GitLastCommitMessageColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.GitCommitAuthorColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowGitCommitAuthorColumn = !preferencesManager.ColumnsViewModel.GitCommitAuthorColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.GitCommitAuthorColumnWidth = preferencesManager.ColumnsViewModel.GitCommitAuthorColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.GitLastCommitShaColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowGitLastCommitShaColumn = !preferencesManager.ColumnsViewModel.GitLastCommitShaColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.GitLastCommitShaColumnWidth = preferencesManager.ColumnsViewModel.GitLastCommitShaColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.DateDeletedColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowDateDeletedColumn = !preferencesManager.ColumnsViewModel.DateDeletedColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.DateDeletedColumnWidth = preferencesManager.ColumnsViewModel.DateDeletedColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.PathColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowPathColumn = !preferencesManager.ColumnsViewModel.PathColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.PathColumnWidth = preferencesManager.ColumnsViewModel.PathColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.OriginalPathColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowOriginalPathColumn = !preferencesManager.ColumnsViewModel.OriginalPathColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.OriginalPathColumnWidth = preferencesManager.ColumnsViewModel.OriginalPathColumn.UserLengthPixels;
				}
				if (!preferencesManager.ColumnsViewModel.StatusColumn.IsHidden)
				{
					userSettingsService.FoldersSettingsService.ShowSyncStatusColumn = !preferencesManager.ColumnsViewModel.StatusColumn.UserCollapsed;
					userSettingsService.FoldersSettingsService.SyncStatusColumnWidth = preferencesManager.ColumnsViewModel.StatusColumn.UserLengthPixels;
				}
			}
		}

		private static LayoutPreferencesManager ReadLayoutPreferencesFromAds(string folderPath, ulong? frn)
		{
			var str = NativeFileOperationsHelper.ReadStringFromFile($"{folderPath}:files_layoutmode");

			var adsPrefs = SafetyExtensions.IgnoreExceptions(() =>
				string.IsNullOrEmpty(str) ? null : JsonSerializer.Deserialize<LayoutPreferencesManager>(str));

			// Port settings to DB, delete ADS
			WriteLayoutPreferencesToDb(folderPath, frn, adsPrefs);
			NativeFileOperationsHelper.DeleteFileFromApp($"{folderPath}:files_layoutmode");

			return adsPrefs;
		}

		private static LayoutPreferencesManager? ReadLayoutPreferencesFromDb(string folderPath, ulong? frn)
		{
			if (string.IsNullOrEmpty(folderPath))
				return null;

			var dbInstance = GetDbInstance();

			return dbInstance.GetPreferences(folderPath, frn);
		}

		private static LayoutPreferencesManager GetDefaultLayoutPreferences(string folderPath)
		{
			if (string.IsNullOrEmpty(folderPath))
				return LayoutPreferencesManager.DefaultLayoutPreferences;

			if (folderPath == Constants.UserEnvironmentPaths.DownloadsPath)
				// Default for downloads folder is to group by date created
				return new LayoutPreferencesManager()
				{
					DirectoryGroupOption = GroupOption.DateCreated,
					DirectoryGroupDirection = SortDirection.Descending,
					DirectoryGroupByDateUnit = GroupByDateUnit.Year
				};
			else if (LibraryManager.IsLibraryPath(folderPath))
				// Default for libraries is to group by folder path
				return new LayoutPreferencesManager() { DirectoryGroupOption = GroupOption.FolderPath };
			else
				// Either global setting or smart guess
				return LayoutPreferencesManager.DefaultLayoutPreferences;
		}

		public static void SetDefaultLayoutPreferences(ColumnsViewModel columns)
		{
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			userSettingsService.FoldersSettingsService.ShowDateColumn = !columns.DateModifiedColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowDateCreatedColumn = !columns.DateCreatedColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowTypeColumn = !columns.ItemTypeColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowSizeColumn = !columns.SizeColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowFileTagColumn = !columns.TagColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowGitStatusColumn = !columns.GitStatusColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowGitLastCommitDateColumn = !columns.GitLastCommitDateColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowGitLastCommitMessageColumn = !columns.GitLastCommitMessageColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowGitCommitAuthorColumn = !columns.GitCommitAuthorColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowGitLastCommitShaColumn = !columns.GitLastCommitShaColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowDateDeletedColumn = !columns.DateDeletedColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowPathColumn = !columns.PathColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowOriginalPathColumn = !columns.OriginalPathColumn.UserCollapsed;
			userSettingsService.FoldersSettingsService.ShowSyncStatusColumn = !columns.StatusColumn.UserCollapsed;

			userSettingsService.FoldersSettingsService.NameColumnWidth = columns.NameColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.DateModifiedColumnWidth = columns.DateModifiedColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.DateCreatedColumnWidth = columns.DateCreatedColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.TypeColumnWidth = columns.ItemTypeColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.SizeColumnWidth = columns.SizeColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.TagColumnWidth = columns.TagColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.GitStatusColumnWidth = columns.GitStatusColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.GitLastCommitDateColumnWidth = columns.GitLastCommitDateColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.GitLastCommitMessageColumnWidth = columns.GitLastCommitMessageColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.GitCommitAuthorColumnWidth = columns.GitCommitAuthorColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.GitLastCommitShaColumnWidth = columns.GitLastCommitShaColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.DateDeletedColumnWidth = columns.DateDeletedColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.PathColumnWidth = columns.PathColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.OriginalPathColumnWidth = columns.OriginalPathColumn.UserLengthPixels;
			userSettingsService.FoldersSettingsService.SyncStatusColumnWidth = columns.StatusColumn.UserLengthPixels;
		}

		private static void WriteLayoutPreferencesToDb(string folderPath, ulong? frn, LayoutPreferencesManager prefs)
		{
			if (string.IsNullOrEmpty(folderPath))
				return;

			var dbInstance = GetDbInstance();
			if (dbInstance.GetPreferences(folderPath, frn) is null &&
				LayoutPreferencesManager.DefaultLayoutPreferences.Equals(prefs))
			{
				// Do not create setting if it's default
				return;
			}

			dbInstance.SetPreferences(folderPath, frn, prefs);
		}
	}
}
