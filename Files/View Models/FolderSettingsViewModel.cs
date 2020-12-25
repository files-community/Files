using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI;
using Newtonsoft.Json;
using System;
using Windows.Storage;

namespace Files.View_Models
{
    public class FolderSettingsViewModel : ObservableObject
    {
        private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private IShellPage associatedInstance;

        public FolderSettingsViewModel(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
            this.LayoutPreference = new LayoutPreferences();
        }

        public LayoutModes LayoutMode
        {
            get => LayoutPreference.LayoutMode;
            set
            {
                if (SetProperty(ref LayoutPreference.LayoutMode, value, nameof(LayoutMode)))
                {
                    UpdateLayoutPreferencesForPath(associatedInstance.FilesystemViewModel.WorkingDirectory, LayoutPreference);
                }
            }
        }

        private bool isLayoutModeChanging;

        public bool IsLayoutModeChanging
        {
            get => isLayoutModeChanging;
            set => SetProperty(ref isLayoutModeChanging, value);
        }

        public Type GetLayoutType(string folderPath)
        {
            var oldLayoutMode = LayoutPreference.LayoutMode;
            LayoutPreference = GetLayoutPreferencesForPath(folderPath);
            if (oldLayoutMode != LayoutPreference.LayoutMode)
            {
                IsLayoutModeChanging = true;
            }

            Type type = null;
            switch (LayoutMode)
            {
                case LayoutModes.DETAILS_VIEW:
                    type = typeof(GenericFileBrowser);
                    break;

                case LayoutModes.TILES_VIEW:
                    type = typeof(GridViewBrowser);
                    break;

                case LayoutModes.GRID_VIEW:
                    type = typeof(GridViewBrowser);
                    break;

                default:
                    type = typeof(GenericFileBrowser);
                    break;
            }
            return type;
        }

        public event EventHandler LayoutModeChangeRequested;
        public event EventHandler GridViewSizeChangeRequested;

        public RelayCommand ToggleLayoutModeGridViewLarge => new RelayCommand(() =>
        {
            LayoutMode = LayoutModes.GRID_VIEW; // Grid View

            GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeLarge; // Size

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeGridViewMedium => new RelayCommand(() =>
        {
            LayoutMode = LayoutModes.GRID_VIEW; // Grid View

            GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeMedium; // Size

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeGridViewSmall => new RelayCommand(() =>
        {
            LayoutMode = LayoutModes.GRID_VIEW; // Grid View

            GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeSmall; // Size

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeTiles => new RelayCommand(() =>
        {
            LayoutMode = LayoutModes.TILES_VIEW; // Tiles View

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeDetailsView => new RelayCommand(() =>
        {
            LayoutMode = LayoutModes.DETAILS_VIEW; // Details View

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public int GridViewSize
        {
            get => LayoutPreference.GridViewSize;
            set
            {
                if (value < LayoutPreference.GridViewSize) // Size down
                {
                    if (LayoutMode == LayoutModes.TILES_VIEW) // Size down from tiles to list
                    {
                        LayoutMode = 0;
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else if (LayoutMode == LayoutModes.GRID_VIEW && value < Constants.Browser.GridViewBrowser.GridViewSizeSmall) // Size down from grid to tiles
                    {
                        LayoutMode = LayoutModes.TILES_VIEW;
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else if (LayoutMode != LayoutModes.DETAILS_VIEW) // Resize grid view
                    {
                        var newValue = (value >= Constants.Browser.GridViewBrowser.GridViewSizeSmall) ? value : Constants.Browser.GridViewBrowser.GridViewSizeSmall; // Set grid size to allow immediate UI update
                        SetProperty(ref LayoutPreference.GridViewSize, newValue, nameof(GridViewSize));

                        if (LayoutMode != LayoutModes.GRID_VIEW) // Only update layout mode if it isn't already in grid view
                        {
                            LayoutMode = LayoutModes.GRID_VIEW;
                            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            UpdateLayoutPreferencesForPath(associatedInstance.FilesystemViewModel.WorkingDirectory, LayoutPreference);
                        }

                        GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                }
                else if (value > LayoutPreference.GridViewSize) // Size up
                {
                    if (LayoutMode == 0) // Size up from list to tiles
                    {
                        LayoutMode = LayoutModes.TILES_VIEW;
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else // Size up from tiles to grid
                    {
                        var newValue = (LayoutMode == LayoutModes.TILES_VIEW) ? Constants.Browser.GridViewBrowser.GridViewSizeSmall : (value <= Constants.Browser.GridViewBrowser.GridViewSizeMax) ? value : Constants.Browser.GridViewBrowser.GridViewSizeMax; // Set grid size to allow immediate UI update
                        SetProperty(ref LayoutPreference.GridViewSize, newValue, nameof(GridViewSize));

                        if (LayoutMode != LayoutModes.GRID_VIEW) // Only update layout mode if it isn't already in grid view
                        {
                            LayoutMode = LayoutModes.GRID_VIEW;
                            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            UpdateLayoutPreferencesForPath(associatedInstance.FilesystemViewModel.WorkingDirectory, LayoutPreference);
                        }

                        if (value < Constants.Browser.GridViewBrowser.GridViewSizeMax) // Don't request a grid resize if it is already at the max size
                        {
                            GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
        }

        public event EventHandler SortOptionPreferenceUpdated;

        public event EventHandler SortDirectionPreferenceUpdated;

        public SortOption DirectorySortOption
        {
            get => LayoutPreference.DirectorySortOption;
            set
            {
                if (SetProperty(ref LayoutPreference.DirectorySortOption, value, nameof(DirectorySortOption)))
                {
                    UpdateLayoutPreferencesForPath(associatedInstance.FilesystemViewModel.WorkingDirectory, LayoutPreference);
                    SortOptionPreferenceUpdated?.Invoke(this, new EventArgs());
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
                    UpdateLayoutPreferencesForPath(associatedInstance.FilesystemViewModel.WorkingDirectory, LayoutPreference);
                    SortDirectionPreferenceUpdated?.Invoke(this, new EventArgs());
                }
            }
        }

        private static LayoutPreferences GetLayoutPreferencesForPath(string folderPath)
        {
            var str = Helpers.NativeFileOperationsHelper.ReadStringFromFile($"{folderPath}:files_layoutmode");
            if (string.IsNullOrEmpty(str))
            {
                return LayoutPreferences.DefaultLayoutPreferences; // Either global setting or smart guess
            }
            return JsonConvert.DeserializeObject<LayoutPreferences>(str);
        }

        private static void UpdateLayoutPreferencesForPath(string folderPath, LayoutPreferences prefs)
        {
            if (LayoutPreferences.DefaultLayoutPreferences.Equals(prefs))
            {
                Helpers.NativeFileOperationsHelper.DeleteFileFromApp($"{folderPath}:files_layoutmode");
                return; // Do not create setting if it's default
            }
            Helpers.NativeFileOperationsHelper.WriteStringToFile($"{folderPath}:files_layoutmode", JsonConvert.SerializeObject(prefs));
        }

        private LayoutPreferences LayoutPreference { get; set; }

        private class LayoutPreferences
        {
            public SortOption DirectorySortOption;
            public SortDirection DirectorySortDirection;
            public LayoutModes LayoutMode;
            public int GridViewSize;

            public static LayoutPreferences DefaultLayoutPreferences => new LayoutPreferences();

            public LayoutPreferences()
            {
                this.LayoutMode = App.AppSettings.DefaultLayoutMode;
                this.GridViewSize = App.AppSettings.DefaultGridViewSize;
                this.DirectorySortOption = App.AppSettings.DefaultDirectorySortOption;
                this.DirectorySortDirection = App.AppSettings.DefaultDirectorySortDirection;
            }

            public LayoutPreferences(LayoutModes layoutMode, int gridViewSize, SortOption sortOption, SortDirection sortDirection)
            {
                this.LayoutMode = layoutMode;
                this.GridViewSize = gridViewSize;
                this.DirectorySortOption = sortOption;
                this.DirectorySortDirection = sortDirection;
            }

            public static LayoutPreferences FromCompositeValue(ApplicationDataCompositeValue compositeValue)
            {
                var layoutPreference = new LayoutPreferences();
                layoutPreference.LayoutMode = (LayoutModes)(int)compositeValue[nameof(LayoutMode)];
                layoutPreference.GridViewSize = (int)compositeValue[nameof(GridViewSize)];
                layoutPreference.DirectorySortOption = (SortOption)(int)compositeValue[nameof(DirectorySortOption)];
                layoutPreference.DirectorySortDirection = (SortDirection)(int)compositeValue[nameof(DirectorySortDirection)];
                return layoutPreference;
            }

            public ApplicationDataCompositeValue ToCompositeValue()
            {
                var compositeValue = new ApplicationDataCompositeValue();
                compositeValue[nameof(LayoutMode)] = (int)this.LayoutMode;
                compositeValue[nameof(GridViewSize)] = this.GridViewSize;
                compositeValue[nameof(DirectorySortOption)] = (int)this.DirectorySortOption;
                compositeValue[nameof(DirectorySortDirection)] = (int)this.DirectorySortDirection;
                return compositeValue;
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }
                if (obj == this)
                {
                    return true;
                }
                if (obj is LayoutPreferences prefs)
                {
                    return (
                        prefs.LayoutMode == this.LayoutMode &&
                        prefs.GridViewSize == this.GridViewSize &&
                        prefs.DirectorySortOption == this.DirectorySortOption &&
                        prefs.DirectorySortDirection == this.DirectorySortDirection);
                }
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        public enum LayoutModes
        {
            DETAILS_VIEW = 0,
            TILES_VIEW,
            GRID_VIEW
        }
    }
}
