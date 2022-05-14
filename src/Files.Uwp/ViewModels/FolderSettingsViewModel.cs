using Files.Shared.Enums;
using Files.Uwp.EventArguments;
using Files.Uwp.Helpers;
using Files.Uwp.Helpers.LayoutPreferences;
using Files.Backend.Services.Settings;
using Files.Uwp.Views.LayoutModes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using Windows.Storage;
using System.Threading.Tasks;
using Files.Uwp.Filesystem.StorageItems;
using Files.Uwp.Filesystem;
using IO = System.IO;

namespace Files.Uwp.ViewModels
{
    public class FolderSettingsViewModel : ObservableObject
    {
        private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private static string LayoutSettingsDbPath => IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "layoutsettings.db");

        private static readonly Lazy<LayoutPrefsDb> dbInstance = new(() => new LayoutPrefsDb(LayoutSettingsDbPath, true));
        private static LayoutPrefsDb DbInstance => dbInstance.Value;

        public event EventHandler<LayoutPreferenceEventArgs> LayoutPreferencesUpdateRequired;

        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

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
        }
        public FolderSettingsViewModel(FolderLayoutModes modeOverride) : this() => rootLayoutMode = modeOverride;

        private readonly FolderLayoutModes? rootLayoutMode;

        public bool IsLayoutModeFixed => rootLayoutMode is not null;

        public bool IsAdaptiveLayoutEnabled
        {
            get => !LayoutPreference.IsAdaptiveLayoutOverridden;
            set
            {
                if (SetProperty(ref LayoutPreference.IsAdaptiveLayoutOverridden, !value, nameof(IsAdaptiveLayoutEnabled)))
                {
                    LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
                }
            }
        }

        public FolderLayoutModes LayoutMode
        {
            get => rootLayoutMode ?? LayoutPreference.LayoutMode;
            set
            {
                if (SetProperty(ref LayoutPreference.LayoutMode, value, nameof(LayoutMode)))
                {
                    LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
                }
            }
        }

        public uint GetIconSize()
        {
            if (LayoutMode == FolderLayoutModes.DetailsView)
            {
                return Constants.Browser.DetailsLayoutBrowser.DetailsViewSize; // ListView thumbnail
            }
            if (LayoutMode == FolderLayoutModes.ColumnView)
            {
                return Constants.Browser.ColumnViewBrowser.ColumnViewSize; // ListView thumbnail
            }
            else if (LayoutMode == FolderLayoutModes.TilesView)
            {
                return Constants.Browser.GridViewBrowser.GridViewSizeSmall; // Small thumbnail
            }
            else if (GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeSmall)
            {
                return Constants.Browser.GridViewBrowser.GridViewSizeSmall; // Small thumbnail
            }
            else if (GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeMedium)
            {
                return Constants.Browser.GridViewBrowser.GridViewSizeMedium; // Medium thumbnail
            }
            else if (GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeLarge)
            {
                return Constants.Browser.GridViewBrowser.GridViewSizeLarge; // Large thumbnail
            }
            else
            {
                return Constants.Browser.GridViewBrowser.GridViewSizeMax; // Extra large thumbnail
            }
        }

        private bool isLayoutModeChanging;

        public bool IsLayoutModeChanging
        {
            get => isLayoutModeChanging;
            set => SetProperty(ref isLayoutModeChanging, value);
        }

        public async Task<Type> GetLayoutType(string folderPath)
        {
            var prefsForPath = await GetLayoutPreferencesForPath(folderPath);
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

        public event EventHandler<LayoutModeEventArgs> LayoutModeChangeRequested;

        public event EventHandler GridViewSizeChangeRequested;

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
                {
                    return GridViewSizeKind.Small;
                }
                else if (GridViewSize >= Constants.Browser.GridViewBrowser.GridViewSizeMedium && GridViewSize < Constants.Browser.GridViewBrowser.GridViewSizeLarge)
                {
                    return GridViewSizeKind.Medium;
                }
                else
                {
                    return GridViewSizeKind.Large;
                }
            }
        }

        public int GridViewSize
        {
            get => LayoutPreference.GridViewSize;
            set
            {
                if (value < LayoutPreference.GridViewSize) // Size down
                {
                    if (LayoutMode == FolderLayoutModes.TilesView) // Size down from tiles to list
                    {
                        LayoutPreference.IsAdaptiveLayoutOverridden = true;
                        LayoutMode = FolderLayoutModes.DetailsView;
                        LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
                    }
                    else if (LayoutMode == FolderLayoutModes.GridView && value < Constants.Browser.GridViewBrowser.GridViewSizeSmall) // Size down from grid to tiles
                    {
                        LayoutPreference.IsAdaptiveLayoutOverridden = true;
                        LayoutMode = FolderLayoutModes.TilesView;
                        LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
                    }
                    else if (LayoutMode != FolderLayoutModes.DetailsView) // Resize grid view
                    {
                        var newValue = (value >= Constants.Browser.GridViewBrowser.GridViewSizeSmall) ? value : Constants.Browser.GridViewBrowser.GridViewSizeSmall; // Set grid size to allow immediate UI update
                        SetProperty(ref LayoutPreference.GridViewSize, newValue, nameof(GridViewSize));

                        if (LayoutMode != FolderLayoutModes.GridView) // Only update layout mode if it isn't already in grid view
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
                else if (value > LayoutPreference.GridViewSize) // Size up
                {
                    if (LayoutMode == FolderLayoutModes.DetailsView) // Size up from list to tiles
                    {
                        LayoutPreference.IsAdaptiveLayoutOverridden = true;
                        LayoutMode = FolderLayoutModes.TilesView;
                        LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
                    }
                    else // Size up from tiles to grid
                    {
                        var newValue = (LayoutMode == FolderLayoutModes.TilesView) ? Constants.Browser.GridViewBrowser.GridViewSizeSmall : (value <= Constants.Browser.GridViewBrowser.GridViewSizeMax) ? value : Constants.Browser.GridViewBrowser.GridViewSizeMax; // Set grid size to allow immediate UI update
                        SetProperty(ref LayoutPreference.GridViewSize, newValue, nameof(GridViewSize));

                        if (LayoutMode != FolderLayoutModes.GridView) // Only update layout mode if it isn't already in grid view
                        {
                            LayoutPreference.IsAdaptiveLayoutOverridden = true;
                            LayoutMode = FolderLayoutModes.GridView;
                            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
                        }
                        else
                        {
                            LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
                        }

                        if (value < Constants.Browser.GridViewBrowser.GridViewSizeMax) // Don't request a grid resize if it is already at the max size
                        {
                            GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
        }

        public event EventHandler<SortOption> SortOptionPreferenceUpdated;

        public event EventHandler<GroupOption> GroupOptionPreferenceUpdated;

        public event EventHandler<SortDirection> SortDirectionPreferenceUpdated;

        public event EventHandler<bool> SortDirectoriesAlongsideFilesPreferenceUpdated;

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

        public bool SortDirectoriesAlongsideFiles
        {
            get
            {
                return UserSettingsService.PreferencesSettingsService.AreLayoutPreferencesPerFolder ?
                LayoutPreference.SortDirectoriesAlongsideFiles :
                UserSettingsService.PreferencesSettingsService.ListAndSortDirectoriesAlongsideFiles;
            }
            set
            {
                if (SetProperty(ref LayoutPreference.SortDirectoriesAlongsideFiles, value, nameof(SortDirectoriesAlongsideFiles)))
                {
                    LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
                    SortDirectoriesAlongsideFilesPreferenceUpdated?.Invoke(this, SortDirectoriesAlongsideFiles);
                    if (!UserSettingsService.PreferencesSettingsService.AreLayoutPreferencesPerFolder)
                    {
                        UserSettingsService.PreferencesSettingsService.ListAndSortDirectoriesAlongsideFiles = value;
                    }
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

        public static async Task<LayoutPreferences> GetLayoutPreferencesForPath(string folderPath)
        {
            IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();
            if (userSettingsService.PreferencesSettingsService.AreLayoutPreferencesPerFolder)
            {
                var folder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(folderPath);
                var folderFRN = await FileTagsHelper.GetFileFRN(folder);
                return ReadLayoutPreferencesFromDb(folderPath.TrimPath(), folderFRN);
            }

            return LayoutPreferences.DefaultLayoutPreferences;
        }

        public static async Task SetLayoutPreferencesForPath(string folderPath, LayoutPreferences prefs)
        {
            IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();
            if (userSettingsService.PreferencesSettingsService.AreLayoutPreferencesPerFolder)
            {
                var folder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(folderPath);
                var folderFRN = await FileTagsHelper.GetFileFRN(folder);
                WriteLayoutPreferencesToDb(folderPath.TrimPath(), folderFRN, prefs);
            }
            else
            {
                userSettingsService.LayoutSettingsService.DefaultLayoutMode = prefs.LayoutMode;
                userSettingsService.LayoutSettingsService.DefaultGridViewSize = prefs.GridViewSize;
                // Do not save OriginalPath as global sort option (only works in recycle bin)
                if (prefs.DirectorySortOption != SortOption.OriginalFolder &&
                    prefs.DirectorySortOption != SortOption.DateDeleted &&
                    prefs.DirectorySortOption != SortOption.SyncStatus)
                {
                    userSettingsService.LayoutSettingsService.DefaultDirectorySortOption = prefs.DirectorySortOption;
                }
                if (prefs.DirectoryGroupOption != GroupOption.OriginalFolder &&
                    prefs.DirectoryGroupOption != GroupOption.DateDeleted &&
                    prefs.DirectoryGroupOption != GroupOption.FolderPath &&
                    prefs.DirectoryGroupOption != GroupOption.SyncStatus)
                {
                    userSettingsService.LayoutSettingsService.DefaultDirectoryGroupOption = prefs.DirectoryGroupOption;
                }
                userSettingsService.LayoutSettingsService.DefaultDirectorySortDirection = prefs.DirectorySortDirection;
                userSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles = prefs.SortDirectoriesAlongsideFiles;
                userSettingsService.LayoutSettingsService.ShowDateColumn = !prefs.ColumnsViewModel.DateModifiedColumn.UserCollapsed;
                userSettingsService.LayoutSettingsService.ShowDateCreatedColumn = !prefs.ColumnsViewModel.DateCreatedColumn.UserCollapsed;
                userSettingsService.LayoutSettingsService.ShowTypeColumn = !prefs.ColumnsViewModel.ItemTypeColumn.UserCollapsed;
                userSettingsService.LayoutSettingsService.ShowSizeColumn = !prefs.ColumnsViewModel.SizeColumn.UserCollapsed;
                userSettingsService.LayoutSettingsService.ShowFileTagColumn = !prefs.ColumnsViewModel.TagColumn.UserCollapsed;
            }
        }

        private static LayoutPreferences ReadLayoutPreferencesFromDb(string folderPath, ulong? frn)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return LayoutPreferences.DefaultLayoutPreferences;
            }

            IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();

            if (DbInstance.GetPreferences(folderPath, frn) is LayoutPreferences storedPrefs)
            {
                return storedPrefs;
            }
            else if (folderPath == CommonPaths.DownloadsPath)
            {
                // Default for downloads folder is to group by date created
                return new LayoutPreferences
                {
                    LayoutMode = userSettingsService.LayoutSettingsService.DefaultLayoutMode,
                    GridViewSize = userSettingsService.LayoutSettingsService.DefaultGridViewSize,
                    DirectorySortOption = userSettingsService.LayoutSettingsService.DefaultDirectorySortOption,
                    DirectorySortDirection = userSettingsService.LayoutSettingsService.DefaultDirectorySortDirection,
                    SortDirectoriesAlongsideFiles = userSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles,
                    ColumnsViewModel = new ColumnsViewModel(),
                    DirectoryGroupOption = GroupOption.DateCreated,
                };
            }
            else if (LibraryHelper.IsLibraryPath(folderPath))
            {
                // Default for libraries is to group by folder path
                return new LayoutPreferences
                {
                    LayoutMode = userSettingsService.LayoutSettingsService.DefaultLayoutMode,
                    GridViewSize = userSettingsService.LayoutSettingsService.DefaultGridViewSize,
                    DirectorySortOption = userSettingsService.LayoutSettingsService.DefaultDirectorySortOption,
                    DirectorySortDirection = userSettingsService.LayoutSettingsService.DefaultDirectorySortDirection,
                    SortDirectoriesAlongsideFiles = userSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles,
                    ColumnsViewModel = new ColumnsViewModel(),
                    DirectoryGroupOption = GroupOption.FolderPath,
                };
            }
            else
            {
                return LayoutPreferences.DefaultLayoutPreferences; // Either global setting or smart guess
            }
        }

        private static void WriteLayoutPreferencesToDb(string folderPath, ulong? frn, LayoutPreferences prefs)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            if (DbInstance.GetPreferences(folderPath, frn) is null)
            {
                if (prefs.Equals(LayoutPreferences.DefaultLayoutPreferences))
                {
                    return; // Do not create setting if it's default
                }
            }
            DbInstance.SetPreferences(folderPath, frn, prefs);
        }

        private LayoutPreferences layoutPreference;

        public LayoutPreferences LayoutPreference
        {
            get => layoutPreference;
            private set
            {
                if (SetProperty(ref layoutPreference, value))
                {
                    OnPropertyChanged(nameof(DirectoryGroupOption));
                    OnPropertyChanged(nameof(DirectorySortOption));
                    OnPropertyChanged(nameof(DirectorySortDirection));
                    OnPropertyChanged(nameof(SortDirectoriesAlongsideFiles));
                }
            }
        }

        public void ToggleLayoutModeGridViewLarge(bool manuallySet)
        {
            IsAdaptiveLayoutEnabled &= !manuallySet;

            LayoutMode = FolderLayoutModes.GridView; // Grid View

            GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeLarge; // Size

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.GridView, GridViewSize));
        }

        public void ToggleLayoutModeColumnView(bool manuallySet)
        {
            IsAdaptiveLayoutEnabled &= !manuallySet;

            LayoutMode = FolderLayoutModes.ColumnView; // Column View

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.ColumnView, GridViewSize));
        }

        public void ToggleLayoutModeGridViewMedium(bool manuallySet)
        {
            IsAdaptiveLayoutEnabled &= !manuallySet;

            LayoutMode = FolderLayoutModes.GridView; // Grid View

            GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeMedium; // Size

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.GridView, GridViewSize));
        }

        public void ToggleLayoutModeGridViewSmall(bool manuallySet)
        {
            IsAdaptiveLayoutEnabled &= !manuallySet;

            LayoutMode = FolderLayoutModes.GridView; // Grid View

            GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeSmall; // Size

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.GridView, GridViewSize));
        }

        public void ToggleLayoutModeGridView(int size)
        {
            LayoutMode = FolderLayoutModes.GridView; // Grid View

            GridViewSize = size; // Size

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
        }

        public void ToggleLayoutModeTiles(bool manuallySet)
        {
            IsAdaptiveLayoutEnabled &= !manuallySet;

            LayoutMode = FolderLayoutModes.TilesView; // Tiles View

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.TilesView, GridViewSize));
        }

        public void ToggleLayoutModeDetailsView(bool manuallySet)
        {
            IsAdaptiveLayoutEnabled &= !manuallySet;

            LayoutMode = FolderLayoutModes.DetailsView; // Details View

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.DetailsView, GridViewSize));
        }

        public void ToggleLayoutModeAdaptive()
        {
            IsAdaptiveLayoutEnabled = true; // Adaptive
        }

        private void ChangeGroupOption(GroupOption option) => DirectoryGroupOption = option;
    }    
}
