using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.View_Models
{
    public class FolderSettingsViewModel : ObservableObject
    {
        private IShellPage associatedInstance;

        public FolderSettingsViewModel(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
            DetectGridViewSize();
        }

        private int layoutMode;
        public int LayoutMode
        {
            get => layoutMode; // Details View
            set => SetProperty(ref layoutMode, value);
        }

        public Type GetLayoutType(string folderPath)
        {
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

        private void DetectGridViewSize()
        {
            //gridViewSize = Get(Constants.Browser.GridViewBrowser.GridViewSizeSmall, "GridViewSize"); // Get GridView Size
        }

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
                        //Set(0, "LayoutMode");
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else if (LayoutMode == 2 && value < Constants.Browser.GridViewBrowser.GridViewSizeSmall) // Size down from grid to tiles
                    {
                        LayoutMode = 1;
                        //Set(1, "LayoutMode");
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else if (LayoutMode != 0) // Resize grid view
                    {
                        var newValue = (value >= Constants.Browser.GridViewBrowser.GridViewSizeSmall) ? value : Constants.Browser.GridViewBrowser.GridViewSizeSmall; // Set grid size to allow immediate UI update
                        //Set(value);
                        SetProperty(ref gridViewSize, newValue);

                        if (LayoutMode != 2) // Only update layout mode if it isn't already in grid view
                        {
                            LayoutMode = 2;
                            //Set(2, "LayoutMode");
                            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }

                        GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                }
                else // Size up
                {
                    if (LayoutMode == 0) // Size up from list to tiles
                    {
                        LayoutMode = 1;
                        //Set(1, "LayoutMode");
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else // Size up from tiles to grid
                    {
                        var newValue = (LayoutMode == 1) ? Constants.Browser.GridViewBrowser.GridViewSizeSmall : (value <= Constants.Browser.GridViewBrowser.GridViewSizeMax) ? value : Constants.Browser.GridViewBrowser.GridViewSizeMax; // Set grid size to allow immediate UI update
                        //Set(gridViewSize);
                        SetProperty(ref gridViewSize, newValue);

                        if (LayoutMode != 2) // Only update layout mode if it isn't already in grid view
                        {
                            LayoutMode = 2;
                            //Set(2, "LayoutMode");
                            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }

                        if (value < Constants.Browser.GridViewBrowser.GridViewSizeMax) // Don't request a grid resize if it is already at the max size
                        {
                            GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
        }        
    }
}
