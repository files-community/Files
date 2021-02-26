using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Files.Common;
using System.Diagnostics;
using Files.Enums;
using System.Collections.Generic;
using System.Windows.Input;
using Files.ViewModels.Previews;
using Files.ViewModels;

namespace Files.Helpers
{
    public static class AdaptiveLayoutHelpers
    {
        public enum GridViewSizeMode
        {
            GridViewSmall,
            GridViewMedium,
            GridViewLarge
        }

        public static void SetPreferredGridViewSizeMode(GridViewSizeMode gridViewSizeMode, string forPath, FolderSettingsViewModel folderSettingsViewModel)
        {
            folderSettingsViewModel.LayoutPreference.PreferredGridViewSizeMode = gridViewSizeMode.ToString();

            folderSettingsViewModel.UpdateLayoutPreferencesForPath(forPath, folderSettingsViewModel.LayoutPreference);
        }

        public static GridViewSizeMode GetPreferredGridViewSizeMode(FolderSettingsViewModel folderSettingsViewModel)
        {
            switch (folderSettingsViewModel.LayoutPreference.PreferredGridViewSizeMode)
            {
                case "GridViewSmall":
                    return GridViewSizeMode.GridViewSmall;

                case "GridViewMedium":
                    return GridViewSizeMode.GridViewMedium;

                case "GridViewLarge":
                    return GridViewSizeMode.GridViewLarge;

                default:
                    return GridViewSizeMode.GridViewSmall;
            }
        }

        public static async Task<bool> PredictLayoutMode(IShellPage associatedInstance)
        {
            if (App.AppSettings.AdaptiveLayoutEnabled && !associatedInstance.InstanceViewModel.FolderSettings.AdaptiveLayoutSuggestionOverriden)
            {
                bool desktopIniFound = false;
                GridViewSizeMode preferredGridViewSize = GetPreferredGridViewSizeMode(associatedInstance.InstanceViewModel.FolderSettings);

                ICommand preferredGridLayout = associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall; // Default

                switch (preferredGridViewSize)
                {
                    case GridViewSizeMode.GridViewSmall:
                        preferredGridLayout = associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall;
                        break;

                    case GridViewSizeMode.GridViewMedium:
                        preferredGridLayout = associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewMedium;
                        break;

                    case GridViewSizeMode.GridViewLarge:
                        preferredGridLayout = associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewLarge;
                        break;
                }

                if (associatedInstance.ServiceConnection != null)
                {
                    AppServiceResponse response = await associatedInstance.ServiceConnection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "FileOperation" },
                        { "fileop", "GetDesktopIniProperties" },
                        { "FilePath", System.IO.Path.Combine(associatedInstance.FilesystemViewModel.CurrentFolder.ItemPath, "desktop.ini") },
                        { "SECTION", "ViewState" },
                        { "KeyName", "FolderType" }
                    });

                    if (response.Status == AppServiceResponseStatus.Success)
                    {
                        string status = response.Message.Get("Status", string.Empty);

                        if (status == "Success")
                        {
                            string result = response.Message.Get("Props", string.Empty);

                            switch (result)
                            {
                                case "Documents":
                                    {
                                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeTiles.Execute(false);
                                        break;
                                    }

                                case "Pictures":
                                    {
                                        preferredGridLayout.Execute(false);
                                        break;
                                    }

                                case "Music":
                                    {
                                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView.Execute(false);
                                        break;
                                    }

                                case "Videos":
                                    {
                                        preferredGridLayout.Execute(false);
                                        break;
                                    }

                                default:
                                    {
                                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView.Execute(false);
                                        break;
                                    }
                            }

                            desktopIniFound = true;
                        }
                        else // error trying to get the properties
                        {
                            string exception = response.Message.Get("Exception", string.Empty);
                        }
                    }
                }
                
                if (desktopIniFound)
                {
                    return true;
                }
                if (associatedInstance.FilesystemViewModel.FilesAndFolders.Count == 0)
                {
                    return false;
                }

                int imagesAndVideosCount = associatedInstance.FilesystemViewModel.FilesAndFolders.Where((item) =>
                    
                    !string.IsNullOrEmpty(item.FileExtension)

                    // Images
                    && (ImagePreviewViewModel.Extensions.Any((ext) => item.FileExtension.Equals(ext, StringComparison.OrdinalIgnoreCase))

                    // Audio & Video
                    || MediaPreviewViewModel.Extensions.Any((ext) => item.FileExtension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                    )).Count();

                int foldersCount = associatedInstance.FilesystemViewModel.FilesAndFolders.Where((item) => item.PrimaryItemAttribute == StorageItemTypes.Folder).Count();

                int otherFilesCount = associatedInstance.FilesystemViewModel.FilesAndFolders.Count - (imagesAndVideosCount + foldersCount);

                if (foldersCount > 0)
                { // There are folders in current directory

                    if ((associatedInstance.FilesystemViewModel.FilesAndFolders.Count - imagesAndVideosCount) < (associatedInstance.FilesystemViewModel.FilesAndFolders.Count - 20) || (associatedInstance.FilesystemViewModel.FilesAndFolders.Count <= 20 && imagesAndVideosCount >= 5))
                    { // Most of items are images/videos
                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeTiles.Execute(false);
                    }
                    else
                    {
                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView.Execute(false);
                    }
                }
                else
                { // There are only files

                    if (imagesAndVideosCount == associatedInstance.FilesystemViewModel.FilesAndFolders.Count)
                    { // Only images/videos
                        preferredGridLayout.Execute(false);
                    }
                    else if (otherFilesCount < 20)
                    { // Most of files are images/videos
                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeTiles.Execute(false);
                    }
                    else
                    { // Images/videos and other files
                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView.Execute(false);
                    }
                }

                return true;
            }

            return false;
        }
    }
}
