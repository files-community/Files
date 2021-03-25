using Files.Enums;
using Files.EventArguments;
using Files.Helpers;
using Files.Views.LayoutModes;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI;
using Newtonsoft.Json;
using System;
using System.Linq;
using Windows.Storage;
using static Files.ViewModels.FolderLayoutInformation;

namespace Files.ViewModels
{
    public class FolderSettingsViewModel : ObservableObject
    {
        private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public event EventHandler<LayoutPreferenceEventArgs> LayoutPreferencesUpdateRequired;

        public FolderSettingsViewModel()
        {
            this.LayoutPreference = new LayoutPreferences();

            SetLayoutInformation();
        }

        public FolderLayoutModes LayoutMode
        {
            get => LayoutPreference.LayoutMode;
            set
            {
                if (SetProperty(ref LayoutPreference.LayoutMode, value, nameof(LayoutMode)))
                {
                    SetLayoutInformation();
                    LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
                }
            }
        }

        public uint GetIconSize()
        {
            if (LayoutMode == FolderLayoutModes.DetailsView)
            {
                return Constants.Browser.GenericFileBrowser.DetailsViewSize; // ListView thumbnail
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

        public FolderLayout LastLayoutModeSelected { get; private set; } = FolderLayout.DetailsView;

        private FolderLayoutInformation layoutModeInformation;

        public FolderLayoutInformation LayoutModeInformation
        {
            get => layoutModeInformation;
            set => SetProperty(ref layoutModeInformation, value);
        }

        public void SetLayoutInformation()
        {
            LayoutModeInformation = new FolderLayoutInformation()
            {
                Mode = LayoutMode,
                SizeKind = GridViewSizeKind
            };
        }

        private bool isLayoutModeChanging;

        public bool IsLayoutModeChanging
        {
            get => isLayoutModeChanging;
            set => SetProperty(ref isLayoutModeChanging, value);
        }

        public Type GetLayoutType(string folderPath, bool isPageNavigationInProgress = true)
        {
            var prefsForPath = GetLayoutPreferencesForPath(folderPath);
            if (isPageNavigationInProgress)
            {
                if (LayoutPreference.LayoutMode != prefsForPath.LayoutMode)
                {
                    IsLayoutModeChanging = true;
                }
                LayoutPreference = prefsForPath;
            }

            Type type = null;
            switch (prefsForPath.LayoutMode)
            {
                case FolderLayoutModes.DetailsView:
                    type = typeof(GenericFileBrowser);
                    break;

                case FolderLayoutModes.TilesView:
                    type = typeof(GridViewBrowser);
                    break;

                case FolderLayoutModes.GridView:
                    type = typeof(GridViewBrowser);
                    break;

                default:
                    type = typeof(GenericFileBrowser);
                    break;
            }
            return type;
        }

        public event EventHandler<LayoutModeEventArgs> LayoutModeChangeRequested;

        public event EventHandler GridViewSizeChangeRequested;

        public void SwitchAdaptiveLayout(bool enable)
        {
            if (enable)
            {
                LayoutPreference.IsAdaptiveLayoutOverridden = false;
                LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference, true));
            }
            else
            {
                LayoutPreference.IsAdaptiveLayoutOverridden = true;
                LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
            }
        }

        public RelayCommand<bool> ToggleLayoutModeGridViewLarge => new RelayCommand<bool>((manuallySet) =>
        {
            if (App.AppSettings.AreLayoutPreferencesPerFolder && App.AppSettings.AdaptiveLayoutEnabled)
            {
                if (LastLayoutModeSelected == FolderLayout.GridViewLarge && LayoutPreference.IsAdaptiveLayoutOverridden)
                {
                    SwitchAdaptiveLayout(true);
                    return;
                }
                else if (LastLayoutModeSelected == FolderLayout.GridViewSmall || LastLayoutModeSelected == FolderLayout.GridViewMedium || LastLayoutModeSelected == FolderLayout.GridViewLarge)
                {
                    // Override preferred gridview size
                }
                else if (manuallySet)
                {
                    // Override preferred layout mode
                    SwitchAdaptiveLayout(false);
                }
            }

            LayoutMode = FolderLayoutModes.GridView; // Grid View

            GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeLarge; // Size

            LastLayoutModeSelected = FolderLayout.GridViewLarge;

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
        });

        public RelayCommand<bool> ToggleLayoutModeGridViewMedium => new RelayCommand<bool>((manuallySet) =>
        {
            if (App.AppSettings.AreLayoutPreferencesPerFolder && App.AppSettings.AdaptiveLayoutEnabled)
            {
                if (LastLayoutModeSelected == FolderLayout.GridViewMedium && LayoutPreference.IsAdaptiveLayoutOverridden)
                {
                    SwitchAdaptiveLayout(true);
                    return;
                }
                else if (LastLayoutModeSelected == FolderLayout.GridViewSmall || LastLayoutModeSelected == FolderLayout.GridViewMedium || LastLayoutModeSelected == FolderLayout.GridViewLarge)
                {
                    // Override preferred gridview size
                }
                else if (manuallySet)
                {
                    // Override preferred layout mode
                    SwitchAdaptiveLayout(false);
                }
            }

            LayoutMode = FolderLayoutModes.GridView; // Grid View

            GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeMedium; // Size

            LastLayoutModeSelected = FolderLayout.GridViewMedium;

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
        });

        public RelayCommand<bool> ToggleLayoutModeGridViewSmall => new RelayCommand<bool>((manuallySet) =>
        {
            if (App.AppSettings.AreLayoutPreferencesPerFolder && App.AppSettings.AdaptiveLayoutEnabled)
            {
                if (LastLayoutModeSelected == FolderLayout.GridViewSmall && LayoutPreference.IsAdaptiveLayoutOverridden)
                {
                    SwitchAdaptiveLayout(true);
                    return;
                }
                else if (LastLayoutModeSelected == FolderLayout.GridViewSmall || LastLayoutModeSelected == FolderLayout.GridViewMedium || LastLayoutModeSelected == FolderLayout.GridViewLarge)
                {
                    // Override preferred gridview size
                }
                else if (manuallySet)
                {
                    // Override preferred layout mode
                    SwitchAdaptiveLayout(false);
                }
            }

            LayoutMode = FolderLayoutModes.GridView; // Grid View

            GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeSmall; // Size

            LastLayoutModeSelected = FolderLayout.GridViewSmall;

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
        });

        public RelayCommand<int> ToggleLayoutModeGridView => new RelayCommand<int>((size) =>
        {
            LayoutMode = FolderLayoutModes.GridView; // Grid View

            GridViewSize = size; // Size

            LastLayoutModeSelected = GridViewSizeKind == GridViewSizeKind.Small ? FolderLayout.GridViewSmall :
                                     GridViewSizeKind == GridViewSizeKind.Medium ? FolderLayout.GridViewMedium :
                                     FolderLayout.GridViewLarge;

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
        });

        public RelayCommand<bool> ToggleLayoutModeTiles => new RelayCommand<bool>((manuallySet) =>
        {
            if (App.AppSettings.AreLayoutPreferencesPerFolder && App.AppSettings.AdaptiveLayoutEnabled)
            {
                if (LastLayoutModeSelected == FolderLayout.TilesView && LayoutPreference.IsAdaptiveLayoutOverridden)
                {
                    SwitchAdaptiveLayout(true);
                    return;
                }
                else if (manuallySet)
                {
                    // Override preferred layout mode
                    SwitchAdaptiveLayout(false);
                }
            }

            LayoutMode = FolderLayoutModes.TilesView; // Tiles View

            LastLayoutModeSelected = FolderLayout.TilesView;

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
        });

        public RelayCommand<bool> ToggleLayoutModeDetailsView => new RelayCommand<bool>((manuallySet) =>
        {
            if (App.AppSettings.AreLayoutPreferencesPerFolder && App.AppSettings.AdaptiveLayoutEnabled)
            {
                if (LastLayoutModeSelected == FolderLayout.DetailsView && LayoutPreference.IsAdaptiveLayoutOverridden)
                {
                    SwitchAdaptiveLayout(true);
                    return;
                }
                else if (manuallySet)
                {
                    // Override preferred layout mode
                    SwitchAdaptiveLayout(false);
                }
            }

            LayoutMode = FolderLayoutModes.DetailsView; // Details View

            LastLayoutModeSelected = FolderLayout.DetailsView;

            LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode, GridViewSize));
        });

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

                SetLayoutInformation();
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
                    LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
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
                    LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreference));
                    SortDirectionPreferenceUpdated?.Invoke(this, new EventArgs());
                }
            }
        }

        public static LayoutPreferences GetLayoutPreferencesForPath(string folderPath)
        {
            if (App.AppSettings.AreLayoutPreferencesPerFolder)
            {
                var layoutPrefs = ReadLayoutPreferencesFromAds(folderPath.TrimEnd('\\'));
                return layoutPrefs ?? ReadLayoutPreferencesFromSettings(folderPath.TrimEnd('\\').Replace('\\', '_'));
            }

            return LayoutPreferences.DefaultLayoutPreferences;
        }

        public void UpdateLayoutPreferencesForPath(string folderPath, LayoutPreferences prefs)
        {
            if (App.AppSettings.AreLayoutPreferencesPerFolder)
            {
                // Sanitize the folderPath by removing the trailing '\\'. This has to be performed because paths to drives
                // include an '\\' at the end (unlike paths to folders)
                if (!WriteLayoutPreferencesToAds(folderPath.TrimEnd('\\'), prefs))
                {
                    WriteLayoutPreferencesToSettings(folderPath.TrimEnd('\\').Replace('\\', '_'), prefs);
                }
            }
            else
            {
                App.AppSettings.DefaultLayoutMode = prefs.LayoutMode;
                App.AppSettings.DefaultGridViewSize = prefs.GridViewSize;
                // Do not save OriginalPath as global sort option (only works in recycle bin)
                if (prefs.DirectorySortOption != SortOption.OriginalPath &&
                    prefs.DirectorySortOption != SortOption.DateDeleted)
                {
                    App.AppSettings.DefaultDirectorySortOption = prefs.DirectorySortOption;
                }
                App.AppSettings.DefaultDirectorySortDirection = prefs.DirectorySortDirection;
            }
        }

        private static LayoutPreferences ReadLayoutPreferencesFromAds(string folderPath)
        {
            var str = NativeFileOperationsHelper.ReadStringFromFile($"{folderPath}:files_layoutmode");
            return string.IsNullOrEmpty(str) ? null : JsonConvert.DeserializeObject<LayoutPreferences>(str);
        }

        private static bool WriteLayoutPreferencesToAds(string folderPath, LayoutPreferences prefs)
        {
            if (LayoutPreferences.DefaultLayoutPreferences.Equals(prefs))
            {
                NativeFileOperationsHelper.DeleteFileFromApp($"{folderPath}:files_layoutmode");
                return false;
            }
            return NativeFileOperationsHelper.WriteStringToFile($"{folderPath}:files_layoutmode", JsonConvert.SerializeObject(prefs));
        }

        private static LayoutPreferences ReadLayoutPreferencesFromSettings(string folderPath)
        {
            ApplicationDataContainer dataContainer = localSettings.CreateContainer("LayoutModeContainer", ApplicationDataCreateDisposition.Always);
            folderPath = new string(folderPath.TakeLast(255).ToArray());
            if (dataContainer.Values.ContainsKey(folderPath))
            {
                ApplicationDataCompositeValue adcv = (ApplicationDataCompositeValue)dataContainer.Values[folderPath];
                return LayoutPreferences.FromCompositeValue(adcv);
            }
            else
            {
                return LayoutPreferences.DefaultLayoutPreferences; // Either global setting or smart guess
            }
        }

        private static void WriteLayoutPreferencesToSettings(string folderPath, LayoutPreferences prefs)
        {
            ApplicationDataContainer dataContainer = localSettings.CreateContainer("LayoutModeContainer", ApplicationDataCreateDisposition.Always);
            folderPath = new string(folderPath.TakeLast(255).ToArray());
            if (!dataContainer.Values.ContainsKey(folderPath))
            {
                if (prefs == LayoutPreferences.DefaultLayoutPreferences)
                {
                    return; // Do not create setting if it's default
                }
            }
            dataContainer.Values[folderPath] = prefs.ToCompositeValue();
        }

        public LayoutPreferences LayoutPreference { get; private set; }

        public class LayoutPreferences
        {
            public SortOption DirectorySortOption;
            public SortDirection DirectorySortDirection;
            public FolderLayoutModes LayoutMode;
            public int GridViewSize;

            public bool IsAdaptiveLayoutOverridden;

            public static LayoutPreferences DefaultLayoutPreferences => new LayoutPreferences();

            public LayoutPreferences()
            {
                this.LayoutMode = App.AppSettings.DefaultLayoutMode;
                this.GridViewSize = App.AppSettings.DefaultGridViewSize;
                this.DirectorySortOption = App.AppSettings.DefaultDirectorySortOption;
                this.DirectorySortDirection = App.AppSettings.DefaultDirectorySortDirection;

                this.IsAdaptiveLayoutOverridden = false; // Default is always turned on for every dir
            }

            public static LayoutPreferences FromCompositeValue(ApplicationDataCompositeValue compositeValue)
            {
                return new LayoutPreferences
                {
                    LayoutMode = (FolderLayoutModes)(int)compositeValue[nameof(LayoutMode)],
                    GridViewSize = (int)compositeValue[nameof(GridViewSize)],
                    DirectorySortOption = (SortOption)(int)compositeValue[nameof(DirectorySortOption)],
                    DirectorySortDirection = (SortDirection)(int)compositeValue[nameof(DirectorySortDirection)],
                    IsAdaptiveLayoutOverridden = (bool?)compositeValue[nameof(IsAdaptiveLayoutOverridden)] != null
                };
            }

            public ApplicationDataCompositeValue ToCompositeValue()
            {
                return new ApplicationDataCompositeValue()
                {
                    { nameof(LayoutMode), (int)this.LayoutMode },
                    { nameof(GridViewSize), this.GridViewSize },
                    { nameof(DirectorySortOption), (int)this.DirectorySortOption },
                    { nameof(DirectorySortDirection), (int)this.DirectorySortDirection },
                    { nameof(IsAdaptiveLayoutOverridden), (bool)this.IsAdaptiveLayoutOverridden }
                };
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
                        prefs.DirectorySortDirection == this.DirectorySortDirection &&
                        prefs.IsAdaptiveLayoutOverridden == this.IsAdaptiveLayoutOverridden);
                }
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
    }

    public class FolderLayoutInformation
    {
        public FolderLayoutModes Mode { get; set; }
        public GridViewSizeKind SizeKind { get; set; }

        public enum GridViewSizeKind
        {
            Small,
            Medium,
            Large
        }
    }
}