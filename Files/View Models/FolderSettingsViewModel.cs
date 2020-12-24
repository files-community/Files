using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI;
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
        }

        private int layoutMode = 0; // Details View
        public int LayoutMode
        {
            get => layoutMode;
            set
            {
                if (layoutMode != value)
                {
                    SetLayoutModeForPath(associatedInstance.FilesystemViewModel.WorkingDirectory, value, gridViewSize);
                }
                SetProperty(ref layoutMode, value);
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
            var oldLayoutMode = layoutMode;
            (layoutMode, gridViewSize) = GetLayoutModeForPath(folderPath);
            if (oldLayoutMode != layoutMode)
            {
                IsLayoutModeChanging = true;
            }

            Type type = null;
            switch (LayoutMode)
            {
                case 0:
                    type = typeof(GenericFileBrowser);
                    break;

                case 1:
                    type = typeof(GridViewBrowser);
                    break;

                case 2:
                    type = typeof(GridViewBrowser);
                    break;

                default:
                    type = typeof(GenericFileBrowser);
                    break;
            }
            return type;
        }

        private static (int, int) GetLayoutModeForPath(string folderPath)
        {
            ApplicationDataContainer dataContainer = localSettings.CreateContainer("LayoutModeContainer", ApplicationDataCreateDisposition.Always);
            var fixPath = folderPath.TrimEnd('\\');
            if (dataContainer.Values.ContainsKey(fixPath))
            {
                var val = (Windows.Foundation.Point)dataContainer.Values[fixPath];
                return ((int)val.X, (int)val.Y);
            }
            else
            {
                return (App.AppSettings.DefaultLayoutMode, App.AppSettings.DefaultGridViewSize); // Either global setting or smart guess
            }
        }

        private static void SetLayoutModeForPath(string folderPath, int layoutMode, int gridViewSize)
        {
            ApplicationDataContainer dataContainer = localSettings.CreateContainer("LayoutModeContainer", ApplicationDataCreateDisposition.Always);
            var fixPath = folderPath.TrimEnd('\\');
            if (!dataContainer.Values.ContainsKey(fixPath))
            {
                if ((layoutMode, gridViewSize) == (App.AppSettings.DefaultLayoutMode, App.AppSettings.DefaultGridViewSize))
                {
                    return; // Do not create setting if it's default
                }
            }
            dataContainer.Values[fixPath] = new Windows.Foundation.Point(layoutMode, gridViewSize);
        }

        public event EventHandler LayoutModeChangeRequested;
        public event EventHandler GridViewSizeChangeRequested;

        public RelayCommand ToggleLayoutModeGridViewLarge => new RelayCommand(() =>
        {
            LayoutMode = 2; // Grid View

            GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeLarge; // Size

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeGridViewMedium => new RelayCommand(() =>
        {
            LayoutMode = 2; // Grid View

            GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeMedium; // Size

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeGridViewSmall => new RelayCommand(() =>
        {
            LayoutMode = 2; // Grid View

            GridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeSmall; // Size

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeTiles => new RelayCommand(() =>
        {
            LayoutMode = 1; // Tiles View

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeDetailsView => new RelayCommand(() =>
        {
            LayoutMode = 0; // Details View

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        private int gridViewSize = Constants.Browser.GridViewBrowser.GridViewSizeSmall; // Default Size

        public int GridViewSize
        {
            get => gridViewSize;
            set
            {
                if (value < gridViewSize) // Size down
                {
                    if (LayoutMode == 1) // Size down from tiles to list
                    {
                        LayoutMode = 0;
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else if (LayoutMode == 2 && value < Constants.Browser.GridViewBrowser.GridViewSizeSmall) // Size down from grid to tiles
                    {
                        LayoutMode = 1;
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else if (LayoutMode != 0) // Resize grid view
                    {
                        var newValue = (value >= Constants.Browser.GridViewBrowser.GridViewSizeSmall) ? value : Constants.Browser.GridViewBrowser.GridViewSizeSmall; // Set grid size to allow immediate UI update
                        SetProperty(ref gridViewSize, newValue);

                        if (LayoutMode != 2) // Only update layout mode if it isn't already in grid view
                        {
                            LayoutMode = 2;
                            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            SetLayoutModeForPath(associatedInstance.FilesystemViewModel.WorkingDirectory, layoutMode, newValue);
                        }

                        GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                }
                else if (value > gridViewSize) // Size up
                {
                    if (LayoutMode == 0) // Size up from list to tiles
                    {
                        LayoutMode = 1;
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else // Size up from tiles to grid
                    {
                        var newValue = (LayoutMode == 1) ? Constants.Browser.GridViewBrowser.GridViewSizeSmall : (value <= Constants.Browser.GridViewBrowser.GridViewSizeMax) ? value : Constants.Browser.GridViewBrowser.GridViewSizeMax; // Set grid size to allow immediate UI update
                        SetProperty(ref gridViewSize, newValue);

                        if (LayoutMode != 2) // Only update layout mode if it isn't already in grid view
                        {
                            LayoutMode = 2;
                            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            SetLayoutModeForPath(associatedInstance.FilesystemViewModel.WorkingDirectory, layoutMode, newValue);
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
            get => (SortOption)SortOptionByte;
            set
            {
                SortOptionByte = (byte)value;
                SortOptionPreferenceUpdated?.Invoke(this, new EventArgs());
            }
        }

        public SortDirection DirectorySortDirection
        {
            get => (SortDirection)SortDirectionByte;
            set
            {
                SortDirectionByte = (byte)value;
                SortDirectionPreferenceUpdated?.Invoke(this, new EventArgs());
            }
        }

        private byte sortOptionByte = (byte)0;

        private byte SortOptionByte
        {
            get => sortOptionByte;
            set => SetProperty(ref sortOptionByte, value);
        }

        private byte sortDirectionByte = 0;

        private byte SortDirectionByte
        {
            get => sortDirectionByte;
            set => SetProperty(ref sortDirectionByte, value);
        }
    }
}
