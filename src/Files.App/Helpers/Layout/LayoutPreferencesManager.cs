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

		/// <summary>
		/// Gets or sets the saved icon size for the current layout
		/// </summary>
		public int IconHeight
		{
			get
			{
				return LayoutMode switch
				{
					FolderLayoutModes.DetailsView => LayoutPreferencesItem.IconHeightDetailsView,
					FolderLayoutModes.ListView => LayoutPreferencesItem.IconHeightListView,
					FolderLayoutModes.TilesView => LayoutPreferencesItem.IconHeightTilesView,
					FolderLayoutModes.GridView => LayoutPreferencesItem.IconHeightGridView,
					FolderLayoutModes.ColumnView => LayoutPreferencesItem.IconHeightColumnsView,
					_ => Constants.IconHeights.GridView.Medium,
				};
			}
			set
			{
				switch (LayoutMode)
				{
					case FolderLayoutModes.DetailsView:
						SetProperty(ref LayoutPreferencesItem.IconHeightDetailsView, value, nameof(IconHeight));
						break;
					case FolderLayoutModes.ListView:
						SetProperty(ref LayoutPreferencesItem.IconHeightListView, value, nameof(IconHeight));
						break;
					case FolderLayoutModes.TilesView:
						SetProperty(ref LayoutPreferencesItem.IconHeightTilesView, value, nameof(IconHeight));
						break;
					case FolderLayoutModes.GridView:
						SetProperty(ref LayoutPreferencesItem.IconHeightGridView, value, nameof(IconHeight));
						break;
					case FolderLayoutModes.ColumnView:
						SetProperty(ref LayoutPreferencesItem.IconHeightColumnsView, value, nameof(IconHeight));
						break;
				}

				IconHeightChanged?.Invoke(this, EventArgs.Empty);
				LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
			}
		}

		public void DecreaseLayoutSize()
		{
			switch (LayoutMode)
			{
				case FolderLayoutModes.GridView:
					if (IconHeight > Constants.IconHeights.GridView.Minimum)
						IconHeight -= Constants.IconHeights.GridView.Increment;
					else
					{
						LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
						LayoutMode = FolderLayoutModes.TilesView;
						IconHeight = Constants.IconHeights.TilesView.Regular;
						LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, IconHeight));
					}
					break;
				case FolderLayoutModes.TilesView:
					LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
					LayoutMode = FolderLayoutModes.ListView;
					IconHeight = Constants.IconHeights.ListView.Regular;
					LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, IconHeight));
					break;
				case FolderLayoutModes.ListView:
					LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
					LayoutMode = FolderLayoutModes.DetailsView;
					IconHeight = Constants.IconHeights.DetailsView.Regular;
					LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, IconHeight));
					break;
			}
		}

		public void IncreaseLayoutSize()
		{
			switch (LayoutMode)
			{
				case FolderLayoutModes.DetailsView:
					LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
					LayoutMode = FolderLayoutModes.ListView;
					IconHeight = Constants.IconHeights.ListView.Regular;
					LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, IconHeight));
					break;
				case FolderLayoutModes.ListView:
					LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
					LayoutMode = FolderLayoutModes.TilesView;
					IconHeight = Constants.IconHeights.TilesView.Regular;
					LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, IconHeight));
					break;
				case FolderLayoutModes.TilesView:
					LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
					LayoutMode = FolderLayoutModes.GridView;
					IconHeight = Constants.IconHeights.GridView.Minimum;
					LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, IconHeight));
					break;
				case FolderLayoutModes.GridView:
					if (IconHeight < Constants.IconHeights.GridView.Maximum)
						IconHeight += Constants.IconHeights.GridView.Increment;
					break;
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

		public bool SortFilesFirst
		{
			get => LayoutPreferencesItem.SortFilesFirst;
			set
			{
				if (SetProperty(ref LayoutPreferencesItem.SortFilesFirst, value, nameof(SortFilesFirst)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					SortFilesFirstPreferenceUpdated?.Invoke(this, SortFilesFirst);
				}
			}
		}

		public ColumnsViewModel ColumnsViewModel
		{
			get => LayoutPreferencesItem.ColumnsViewModel;
			set
			{
				SetProperty(ref LayoutPreferencesItem.ColumnsViewModel, value, nameof(ColumnsViewModel));
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
					OnPropertyChanged(nameof(IconHeight));
					OnPropertyChanged(nameof(IsAdaptiveLayoutEnabled));
					OnPropertyChanged(nameof(DirectoryGroupOption));
					OnPropertyChanged(nameof(DirectorySortOption));
					OnPropertyChanged(nameof(DirectorySortDirection));
					OnPropertyChanged(nameof(DirectoryGroupDirection));
					OnPropertyChanged(nameof(DirectoryGroupByDateUnit));
					OnPropertyChanged(nameof(SortDirectoriesAlongsideFiles));
					OnPropertyChanged(nameof(SortFilesFirst));
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
		public event EventHandler<bool>? SortFilesFirstPreferenceUpdated;
		public event EventHandler<LayoutModeEventArgs>? LayoutModeChangeRequested;
		public event EventHandler? IconHeightChanged;

		// Constructors

		public LayoutPreferencesManager()
		{
			LayoutPreferencesItem = new LayoutPreferencesItem();
		}

		public LayoutPreferencesManager(FolderLayoutModes modeOverride) : this()
		{
			_rootLayoutMode = modeOverride;
			LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
		}

		// Methods

		/// <summary>
		/// This will round the current icon size to get the best result from the File Explorer thumbnail system.
		/// 
		/// Details View:
		///		Always uses the Large icon size (32).
		///		
		/// List View:
		///		Always uses the Large icon size (32).
		///		
		/// Columns View:
		///		Always uses the Large icon size (32).
		///		
		/// Tiles View:
		///		Uses a range of icon sizes (64, 72, 96, 128, 180, 256) depending on the selected icon size.
		///		
		/// Grid View:
		///		Uses a range of icon sizes (64, 72, 96, 128, 180, 256) depending on the selected icon size.
		/// </summary>
		public uint GetRoundedIconSize()
		{
			return LayoutMode switch
			{
				FolderLayoutModes.DetailsView
					=> Constants.ShellIconSizes.Large,
				FolderLayoutModes.ListView
					=> Constants.ShellIconSizes.Large,
				FolderLayoutModes.ColumnView
					=> Constants.ShellIconSizes.Large,
				_ when LayoutMode == FolderLayoutModes.TilesView && LayoutPreferencesItem.IconHeightTilesView <= 64 ||
					   LayoutMode == FolderLayoutModes.GridView && LayoutPreferencesItem.IconHeightGridView <= 64
					=> 64,
				_ when LayoutMode == FolderLayoutModes.TilesView && LayoutPreferencesItem.IconHeightTilesView <= 72 ||
					   LayoutMode == FolderLayoutModes.GridView && LayoutPreferencesItem.IconHeightGridView <= 72
					=> 72,
				_ when LayoutMode == FolderLayoutModes.TilesView && LayoutPreferencesItem.IconHeightTilesView <= 96 ||
					   LayoutMode == FolderLayoutModes.GridView && LayoutPreferencesItem.IconHeightGridView <= 96
					=> 96,
				_ when LayoutMode == FolderLayoutModes.TilesView && LayoutPreferencesItem.IconHeightTilesView <= 128 ||
					   LayoutMode == FolderLayoutModes.GridView && LayoutPreferencesItem.IconHeightGridView <= 128
					=> 128,
				_ when LayoutMode == FolderLayoutModes.TilesView && LayoutPreferencesItem.IconHeightTilesView <= 180 ||
					   LayoutMode == FolderLayoutModes.GridView && LayoutPreferencesItem.IconHeightGridView <= 180
					=> 180,
				_ => 256,
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
				FolderLayoutModes.ListView => typeof(GridLayoutPage),
				FolderLayoutModes.TilesView => typeof(GridLayoutPage),
				FolderLayoutModes.GridView => typeof(GridLayoutPage),
				FolderLayoutModes.ColumnView => typeof(ColumnsLayoutPage),
				_ => typeof(DetailsLayoutPage)
			};
		}

		public void ToggleLayoutModeColumnView(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Column View
			LayoutMode = FolderLayoutModes.ColumnView;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.ColumnView, IconHeight));
		}

		public void ToggleLayoutModeGridView(int size, bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Grid View
			LayoutMode = FolderLayoutModes.GridView;

			// Size
			IconHeight = size;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.GridView, IconHeight));
		}

		public void ToggleLayoutModeTiles(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Tiles View
			LayoutMode = FolderLayoutModes.TilesView;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.TilesView, IconHeight));
		}

		public void ToggleLayoutModeList(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// List View
			LayoutMode = FolderLayoutModes.ListView;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.ListView, IconHeight));
		}

		public void ToggleLayoutModeDetailsView(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Details View
			LayoutMode = FolderLayoutModes.DetailsView;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.DetailsView, IconHeight));
		}

		public void ToggleLayoutModeAdaptive()
		{
			// Adaptive
			IsAdaptiveLayoutEnabled = true;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.Adaptive, IconHeight));
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
				case nameof(UserSettingsService.FoldersSettingsService.DefaultSortFilesFirst):
					SortFilesFirst = preferencesItem.SortFilesFirst;
					break;
				case nameof(UserSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories):
					LayoutPreferencesItem = preferencesItem;
					LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, IconHeight));
					break;
				case nameof(UserSettingsService.FoldersSettingsService.DefaultLayoutMode):
				case nameof(UserSettingsService.LayoutSettingsService.DefaultIconHeightDetailsView):
				case nameof(UserSettingsService.LayoutSettingsService.DefaultIconHeightListView):
				case nameof(UserSettingsService.LayoutSettingsService.DefaulIconHeightTilesView):
				case nameof(UserSettingsService.LayoutSettingsService.DefaulIconHeightGridView):
				case nameof(UserSettingsService.LayoutSettingsService.DefaultIconHeightColumnsView):
					LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, IconHeight));
					break;
			}
		}

		// Static methods

		public static LayoutPreferencesDatabaseManager GetDatabaseManagerInstance()
		{
			return _databaseInstance.Value;
		}

		public static void SetDefaultLayoutPreferences(ColumnsViewModel columns)
		{
			UserSettingsService.FoldersSettingsService.ShowDateColumn = !columns.DateModifiedColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowDateCreatedColumn = !columns.DateCreatedColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowTypeColumn = !columns.ItemTypeColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowSizeColumn = !columns.SizeColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowFileTagColumn = !columns.TagColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowGitStatusColumn = !columns.GitStatusColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowGitLastCommitDateColumn = !columns.GitLastCommitDateColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowGitLastCommitMessageColumn = !columns.GitLastCommitMessageColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowGitCommitAuthorColumn = !columns.GitCommitAuthorColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowGitLastCommitShaColumn = !columns.GitLastCommitShaColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowDateDeletedColumn = !columns.DateDeletedColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowPathColumn = !columns.PathColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowOriginalPathColumn = !columns.OriginalPathColumn.UserCollapsed;
			UserSettingsService.FoldersSettingsService.ShowSyncStatusColumn = !columns.StatusColumn.UserCollapsed;

			UserSettingsService.FoldersSettingsService.NameColumnWidth = columns.NameColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.DateModifiedColumnWidth = columns.DateModifiedColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.DateCreatedColumnWidth = columns.DateCreatedColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.TypeColumnWidth = columns.ItemTypeColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.SizeColumnWidth = columns.SizeColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.TagColumnWidth = columns.TagColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.GitStatusColumnWidth = columns.GitStatusColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.GitLastCommitDateColumnWidth = columns.GitLastCommitDateColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.GitLastCommitMessageColumnWidth = columns.GitLastCommitMessageColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.GitCommitAuthorColumnWidth = columns.GitCommitAuthorColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.GitLastCommitShaColumnWidth = columns.GitLastCommitShaColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.DateDeletedColumnWidth = columns.DateDeletedColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.PathColumnWidth = columns.PathColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.OriginalPathColumnWidth = columns.OriginalPathColumn.UserLengthPixels;
			UserSettingsService.FoldersSettingsService.SyncStatusColumnWidth = columns.StatusColumn.UserLengthPixels;
		}

		public static void SetLayoutPreferencesForPath(string path, LayoutPreferencesItem preferencesItem)
		{
			if (!UserSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories)
			{
				var folderFRN = NativeFileOperationsHelper.GetFolderFRN(path);
				var trimmedFolderPath = path.TrimPath();
				if (trimmedFolderPath is not null)
					SetLayoutPreferencesToDatabase(trimmedFolderPath, folderFRN, preferencesItem);
			}
			else
			{
				UserSettingsService.FoldersSettingsService.DefaultLayoutMode = preferencesItem.LayoutMode;
				UserSettingsService.LayoutSettingsService.DefaultIconHeightDetailsView = preferencesItem.IconHeightDetailsView;
				UserSettingsService.LayoutSettingsService.DefaultIconHeightListView = preferencesItem.IconHeightListView;
				UserSettingsService.LayoutSettingsService.DefaulIconHeightTilesView = preferencesItem.IconHeightTilesView;
				UserSettingsService.LayoutSettingsService.DefaulIconHeightGridView = preferencesItem.IconHeightGridView;
				UserSettingsService.LayoutSettingsService.DefaultIconHeightColumnsView = preferencesItem.IconHeightColumnsView;

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
				UserSettingsService.FoldersSettingsService.DefaultSortFilesFirst = preferencesItem.SortFilesFirst;

				UserSettingsService.FoldersSettingsService.NameColumnWidth = preferencesItem.ColumnsViewModel.NameColumn.UserLengthPixels;

				if (!preferencesItem.ColumnsViewModel.DateModifiedColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowDateColumn = !preferencesItem.ColumnsViewModel.DateModifiedColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.DateModifiedColumnWidth = preferencesItem.ColumnsViewModel.DateModifiedColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.DateCreatedColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowDateCreatedColumn = !preferencesItem.ColumnsViewModel.DateCreatedColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.DateCreatedColumnWidth = preferencesItem.ColumnsViewModel.DateCreatedColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.ItemTypeColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowTypeColumn = !preferencesItem.ColumnsViewModel.ItemTypeColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.TypeColumnWidth = preferencesItem.ColumnsViewModel.ItemTypeColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.SizeColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowSizeColumn = !preferencesItem.ColumnsViewModel.SizeColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.SizeColumnWidth = preferencesItem.ColumnsViewModel.SizeColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.TagColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowFileTagColumn = !preferencesItem.ColumnsViewModel.TagColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.TagColumnWidth = preferencesItem.ColumnsViewModel.TagColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitStatusColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowGitStatusColumn = !preferencesItem.ColumnsViewModel.GitStatusColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.GitStatusColumnWidth = preferencesItem.ColumnsViewModel.GitStatusColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitLastCommitDateColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowGitLastCommitDateColumn = !preferencesItem.ColumnsViewModel.GitLastCommitDateColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.GitLastCommitDateColumnWidth = preferencesItem.ColumnsViewModel.GitLastCommitDateColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitLastCommitMessageColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowGitLastCommitMessageColumn = !preferencesItem.ColumnsViewModel.GitLastCommitMessageColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.GitLastCommitMessageColumnWidth = preferencesItem.ColumnsViewModel.GitLastCommitMessageColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitCommitAuthorColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowGitCommitAuthorColumn = !preferencesItem.ColumnsViewModel.GitCommitAuthorColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.GitCommitAuthorColumnWidth = preferencesItem.ColumnsViewModel.GitCommitAuthorColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitLastCommitShaColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowGitLastCommitShaColumn = !preferencesItem.ColumnsViewModel.GitLastCommitShaColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.GitLastCommitShaColumnWidth = preferencesItem.ColumnsViewModel.GitLastCommitShaColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.DateDeletedColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowDateDeletedColumn = !preferencesItem.ColumnsViewModel.DateDeletedColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.DateDeletedColumnWidth = preferencesItem.ColumnsViewModel.DateDeletedColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.PathColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowPathColumn = !preferencesItem.ColumnsViewModel.PathColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.PathColumnWidth = preferencesItem.ColumnsViewModel.PathColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.OriginalPathColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowOriginalPathColumn = !preferencesItem.ColumnsViewModel.OriginalPathColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.OriginalPathColumnWidth = preferencesItem.ColumnsViewModel.OriginalPathColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.StatusColumn.IsHidden)
				{
					UserSettingsService.FoldersSettingsService.ShowSyncStatusColumn = !preferencesItem.ColumnsViewModel.StatusColumn.UserCollapsed;
					UserSettingsService.FoldersSettingsService.SyncStatusColumnWidth = preferencesItem.ColumnsViewModel.StatusColumn.UserLengthPixels;
				}
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
