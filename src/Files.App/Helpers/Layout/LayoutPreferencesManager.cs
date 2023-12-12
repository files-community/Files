// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Text.Json;
using Windows.Storage;

namespace Files.App.Data.Models
{
	/// <summary>
	/// Represents manager for layout preferences settings.
	/// </summary>
	public class LayoutPreferencesManager : ObservableObject
	{
		// Dependency injections

		private static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Fields

		private static readonly Lazy<LayoutPreferencesDatabaseManager> _databaseInstance =
			new(() => new LayoutPreferencesDatabaseManager(LayoutSettingsDbPath, true));

		private readonly FolderLayoutModes? _rootLayoutMode;

		// Properties

		public static string LayoutSettingsDbPath
			=> SystemIO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "user_settings.db");

		public bool IsLayoutModeFixed
			=> _rootLayoutMode is not null;

		public bool IsAdaptiveLayoutEnabled
		{
			get => !LayoutPreferencesItem.IsAdaptiveLayoutOverridden;
			set
			{
				if (SetProperty(ref LayoutPreferencesItem.IsAdaptiveLayoutOverridden, !value, nameof(IsAdaptiveLayoutEnabled)))
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem, true));
			}
		}

		public int GridViewSize
		{
			get => LayoutPreferencesItem.GridViewSize;
			set
			{
				// Size down
				if (value < LayoutPreferencesItem.GridViewSize)
				{
					// Size down from tiles to list
					if (LayoutMode == FolderLayoutModes.TilesView)
					{
						LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
						LayoutMode = FolderLayoutModes.DetailsView;
						LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
					}
					// Size down from grid to tiles
					else if (LayoutMode == FolderLayoutModes.GridView && value < Constants.Browser.GridViewBrowser.GridViewSizeSmall)
					{
						LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
						LayoutMode = FolderLayoutModes.TilesView;
						LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
					}
					// Resize grid view
					else if (LayoutMode != FolderLayoutModes.DetailsView)
					{
						// Set grid size to allow immediate UI update
						var newValue = (value >= Constants.Browser.GridViewBrowser.GridViewSizeSmall) ? value : Constants.Browser.GridViewBrowser.GridViewSizeSmall;
						SetProperty(ref LayoutPreferencesItem.GridViewSize, newValue, nameof(GridViewSize));

						// Only update layout mode if it isn't already in grid view
						if (LayoutMode != FolderLayoutModes.GridView)
						{
							LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
							LayoutMode = FolderLayoutModes.GridView;
							LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
						}
						else
						{
							LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
						}

						GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
					}
				}
				// Size up
				else if (value > LayoutPreferencesItem.GridViewSize)
				{
					// Size up from list to tiles
					if (LayoutMode == FolderLayoutModes.DetailsView)
					{
						LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
						LayoutMode = FolderLayoutModes.TilesView;
						LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
					}
					else // Size up from tiles to grid
					{
						// Set grid size to allow immediate UI update
						var newValue = (LayoutMode == FolderLayoutModes.TilesView) ? Constants.Browser.GridViewBrowser.GridViewSizeSmall : (value <= Constants.Browser.GridViewBrowser.GridViewSizeMax) ? value : Constants.Browser.GridViewBrowser.GridViewSizeMax;
						SetProperty(ref LayoutPreferencesItem.GridViewSize, newValue, nameof(GridViewSize));

						// Only update layout mode if it isn't already in grid view
						if (LayoutMode != FolderLayoutModes.GridView)
						{
							LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
							LayoutMode = FolderLayoutModes.GridView;
							LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
						}
						else
						{
							LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
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
			get => _rootLayoutMode ?? LayoutPreferencesItem.LayoutMode;
			set
			{
				if (SetProperty(ref LayoutPreferencesItem.LayoutMode, value, nameof(LayoutMode)))
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
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
			get => LayoutPreferencesItem.DirectorySortOption;
			set
			{
				if (SetProperty(ref LayoutPreferencesItem.DirectorySortOption, value, nameof(DirectorySortOption)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					SortOptionPreferenceUpdated?.Invoke(this, DirectorySortOption);
				}
			}
		}

		public GroupOption DirectoryGroupOption
		{
			get => LayoutPreferencesItem.DirectoryGroupOption;
			set
			{
				if (SetProperty(ref LayoutPreferencesItem.DirectoryGroupOption, value, nameof(DirectoryGroupOption)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					GroupOptionPreferenceUpdated?.Invoke(this, DirectoryGroupOption);
				}
			}
		}

		public SortDirection DirectorySortDirection
		{
			get => LayoutPreferencesItem.DirectorySortDirection;
			set
			{
				if (SetProperty(ref LayoutPreferencesItem.DirectorySortDirection, value, nameof(DirectorySortDirection)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					SortDirectionPreferenceUpdated?.Invoke(this, DirectorySortDirection);
				}
			}
		}

		public SortDirection DirectoryGroupDirection
		{
			get => LayoutPreferencesItem.DirectoryGroupDirection;
			set
			{
				if (SetProperty(ref LayoutPreferencesItem.DirectoryGroupDirection, value, nameof(DirectoryGroupDirection)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					GroupDirectionPreferenceUpdated?.Invoke(this, DirectoryGroupDirection);
				}
			}
		}

		public GroupByDateUnit DirectoryGroupByDateUnit
		{
			get => LayoutPreferencesItem.DirectoryGroupByDateUnit;
			set
			{
				if (SetProperty(ref LayoutPreferencesItem.DirectoryGroupByDateUnit, value, nameof(DirectoryGroupByDateUnit)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					GroupByDateUnitPreferenceUpdated?.Invoke(this, DirectoryGroupByDateUnit);
				}
			}
		}

		public bool SortDirectoriesAlongsideFiles
		{
			get => LayoutPreferencesItem.SortDirectoriesAlongsideFiles;
			set
			{
				if (SetProperty(ref LayoutPreferencesItem.SortDirectoriesAlongsideFiles, value, nameof(SortDirectoriesAlongsideFiles)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					SortDirectoriesAlongsideFilesPreferenceUpdated?.Invoke(this, SortDirectoriesAlongsideFiles);
				}
			}
		}

		public IList<DetailsLayoutColumnItem> ColumnItems
		{
			get => LayoutPreferencesItem.ColumnItems;
			set
			{
				SetProperty(ref LayoutPreferencesItem.ColumnItems, value, nameof(ColumnItems));
				LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
			}
		}

		private bool _IsLayoutModeChanging;
		public bool IsLayoutModeChanging
		{
			get => _IsLayoutModeChanging;
			set => SetProperty(ref _IsLayoutModeChanging, value);
		}

		private LayoutPreferencesItem? _LayoutPreferencesItem;
		public LayoutPreferencesItem LayoutPreferencesItem
		{
			get => _LayoutPreferencesItem!;
			private set
			{
				if (SetProperty(ref _LayoutPreferencesItem, value))
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
					OnPropertyChanged(nameof(ColumnItems));
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

		// Constructor

		public LayoutPreferencesManager(FolderLayoutModes? modeOverride = null)
		{
			LayoutPreferencesItem = new LayoutPreferencesItem();

			if (modeOverride is not null)
			{
				_rootLayoutMode = modeOverride;
				LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
			}
		}

		// Methods

		public uint GetIconSize()
		{
			return LayoutMode switch
			{
				FolderLayoutModes.DetailsView
					=> Constants.Browser.DetailsLayoutBrowser.DetailsViewSize,
				FolderLayoutModes.ColumnView
					=> Constants.Browser.ColumnViewBrowser.ColumnViewSize,
				FolderLayoutModes.TilesView
					=> Constants.Browser.GridViewBrowser.GridViewSizeSmall,
				_ when GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeSmall
					=> Constants.Browser.GridViewBrowser.GridViewSizeSmall,
				_ when GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeMedium
					=> Constants.Browser.GridViewBrowser.GridViewSizeMedium,
				_ when GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeLarge
					=> Constants.Browser.GridViewBrowser.GridViewSizeLarge,
				_ => Constants.Browser.GridViewBrowser.GridViewSizeMax,
			};
		}

		public Type GetLayoutType(string path, bool changeLayoutMode = true)
		{
			var preferencesItem = GetLayoutPreferencesForPath(path);
			if (preferencesItem is null)
				return typeof(DetailsLayoutPage);

			if (changeLayoutMode)
			{
				IsLayoutModeChanging = LayoutPreferencesItem.LayoutMode != preferencesItem.LayoutMode;
				LayoutPreferencesItem = preferencesItem;
			}

			return (preferencesItem.LayoutMode) switch
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

		public void OnDefaultPreferencesChanged(string path, string settingsName)
		{
			var preferencesItem = GetLayoutPreferencesForPath(path);
			if (preferencesItem is null)
				return;

			switch (settingsName)
			{
				case nameof(UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles):
					SortDirectoriesAlongsideFiles = preferencesItem.SortDirectoriesAlongsideFiles;
					break;
				case nameof(UserSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories):
					LayoutPreferencesItem = preferencesItem;
					// TODO: Update layout
					break;
			}
		}

		// Static methods

		public static LayoutPreferencesDatabaseManager GetDatabaseManagerInstance()
		{
			return _databaseInstance.Value;
		}

		public static void SetDefaultLayoutPreferences(IList<DetailsLayoutColumnItem> columns)
		{
			UserSettingsService.LayoutSettingsService.ColumnItems = columns.Select(DetailsLayoutColumnItem.ToModel).ToList();
		}

		public static void SetLayoutPreferencesForPath(string path, LayoutPreferencesItem preferencesItem)
		{
			if (UserSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories)
			{
				UserSettingsService.FoldersSettingsService.DefaultLayoutMode = preferencesItem.LayoutMode;
				UserSettingsService.LayoutSettingsService.DefaultGridViewSize = preferencesItem.GridViewSize;

				// Do not save options which only work in recycle bin or cloud folders or search results as global
				if (preferencesItem.DirectorySortOption != SortOption.Path &&
					preferencesItem.DirectorySortOption != SortOption.OriginalFolder &&
					preferencesItem.DirectorySortOption != SortOption.DateDeleted &&
					preferencesItem.DirectorySortOption != SortOption.SyncStatus)
				{
					UserSettingsService.FoldersSettingsService.DefaultSortOption = preferencesItem.DirectorySortOption;
				}

				if (preferencesItem.DirectoryGroupOption != GroupOption.OriginalFolder &&
					preferencesItem.DirectoryGroupOption != GroupOption.DateDeleted &&
					preferencesItem.DirectoryGroupOption != GroupOption.FolderPath &&
					preferencesItem.DirectoryGroupOption != GroupOption.SyncStatus)
				{
					UserSettingsService.FoldersSettingsService.DefaultGroupOption = preferencesItem.DirectoryGroupOption;
				}

				UserSettingsService.FoldersSettingsService.DefaultDirectorySortDirection = preferencesItem.DirectorySortDirection;
				UserSettingsService.FoldersSettingsService.DefaultDirectoryGroupDirection = preferencesItem.DirectoryGroupDirection;
				UserSettingsService.FoldersSettingsService.DefaultGroupByDateUnit = preferencesItem.DirectoryGroupByDateUnit;
				UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles = preferencesItem.SortDirectoriesAlongsideFiles;

				// Set static layout columns preferences to the json app settings
				UserSettingsService.LayoutSettingsService.ColumnItems =
					preferencesItem.ColumnItems.Select(DetailsLayoutColumnItem.ToModel).ToList();
			}
			else
			{
				var folderFRN = NativeFileOperationsHelper.GetFolderFRN(path);
				var trimmedFolderPath = path.TrimPath();
				if (trimmedFolderPath is not null)
					SetLayoutPreferencesToDatabase(trimmedFolderPath, folderFRN, preferencesItem);
			}
		}

		private static LayoutPreferencesItem? GetLayoutPreferencesForPath(string path)
		{
			if (!UserSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories)
			{
				path = path.TrimPath() ?? string.Empty;

				var folderFRN = NativeFileOperationsHelper.GetFolderFRN(path);

				return GetLayoutPreferencesFromDatabase(path, folderFRN)
					?? GetLayoutPreferencesFromAds(path, folderFRN)
					?? GetDefaultLayoutPreferences(path);
			}

			return new LayoutPreferencesItem();
		}

		private static LayoutPreferencesItem? GetLayoutPreferencesFromAds(string path, ulong? frn)
		{
			var str = NativeFileOperationsHelper.ReadStringFromFile($"{path}:files_layoutmode");

			var layoutPreferences = SafetyExtensions.IgnoreExceptions(() =>
				string.IsNullOrEmpty(str) ? null : JsonSerializer.Deserialize<LayoutPreferencesItem>(str));

			if (layoutPreferences is null)
				return null;

			// Port settings to the database, delete the ADS
			SetLayoutPreferencesToDatabase(path, frn, layoutPreferences);
			NativeFileOperationsHelper.DeleteFileFromApp($"{path}:files_layoutmode");

			return layoutPreferences;
		}

		private static LayoutPreferencesItem? GetLayoutPreferencesFromDatabase(string path, ulong? frn)
		{
			if (string.IsNullOrEmpty(path))
				return null;

			var databaseManager = GetDatabaseManagerInstance();

			return databaseManager.GetPreferences(path, frn);
		}

		private static LayoutPreferencesItem? GetDefaultLayoutPreferences(string path)
		{
			if (string.IsNullOrEmpty(path))
				return new();

			if (path == Constants.UserEnvironmentPaths.DownloadsPath)
			{
				// Default for downloads folder is to group by date created
				return new()
				{
					DirectoryGroupOption = GroupOption.DateCreated,
					DirectoryGroupDirection = SortDirection.Descending,
					DirectoryGroupByDateUnit = GroupByDateUnit.Year
				};
			}
			else if (LibraryManager.IsLibraryPath(path))
			{
				// Default for libraries is to group by folder path
				return new()
				{
					DirectoryGroupOption = GroupOption.FolderPath
				};
			}
			else
			{
				// Either global setting or smart guess
				return new();
			}
		}

		private static void SetLayoutPreferencesToDatabase(string path, ulong? frn, LayoutPreferencesItem preferencesItem)
		{
			if (string.IsNullOrEmpty(path))
				return;

			var dbInstance = GetDatabaseManagerInstance();
			if (dbInstance.GetPreferences(path, frn) is null &&
				new LayoutPreferencesItem().Equals(preferencesItem))
			{
				// Do not create setting if it's default
				return;
			}

			dbInstance.SetPreferences(path, frn, preferencesItem);
		}
	}
}
