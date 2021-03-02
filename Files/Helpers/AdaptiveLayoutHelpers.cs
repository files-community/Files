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

        public static async Task<bool> PredictLayoutMode(FolderSettingsViewModel folderSettings, ItemViewModel filesystemViewModel, AppServiceConnection connection)
        {
            if (App.AppSettings.AdaptiveLayoutEnabled && !folderSettings.AdaptiveLayoutSuggestionOverriden)
            {
                bool desktopIniFound = false;
                GridViewSizeMode preferredGridViewSize = GetPreferredGridViewSizeMode(folderSettings);

                ICommand preferredGridLayout = folderSettings.ToggleLayoutModeGridViewSmall; // Default

                switch (preferredGridViewSize)
                {
                    case GridViewSizeMode.GridViewSmall:
                        preferredGridLayout = folderSettings.ToggleLayoutModeGridViewSmall;
                        break;

                    case GridViewSizeMode.GridViewMedium:
                        preferredGridLayout = folderSettings.ToggleLayoutModeGridViewMedium;
                        break;

                    case GridViewSizeMode.GridViewLarge:
                        preferredGridLayout = folderSettings.ToggleLayoutModeGridViewLarge;
                        break;
                }

                if (connection != null)
                {
                    AppServiceResponse response = await connection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "FileOperation" },
                        { "fileop", "GetDesktopIniProperties" },
                        { "FilePath", System.IO.Path.Combine(filesystemViewModel.CurrentFolder.ItemPath, "desktop.ini") },
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
                                        folderSettings.ToggleLayoutModeTiles.Execute(false);
                                        break;
                                    }

                                case "Pictures":
                                    {
                                        preferredGridLayout.Execute(false);
                                        break;
                                    }

                                case "Music":
                                    {
                                        folderSettings.ToggleLayoutModeDetailsView.Execute(false);
                                        break;
                                    }

                                case "Videos":
                                    {
                                        preferredGridLayout.Execute(false);
                                        break;
                                    }

                                default:
                                    {
                                        folderSettings.ToggleLayoutModeDetailsView.Execute(false);
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
                if (filesystemViewModel.FilesAndFolders.Count == 0)
                {
                    return false;
                }

                int imagesAndVideosCount = filesystemViewModel.FilesAndFolders.Where((item) =>
                    
                    !string.IsNullOrEmpty(item.FileExtension)

                    // Images
                    && (ImagePreviewViewModel.Extensions.Any((ext) => item.FileExtension.Equals(ext, StringComparison.OrdinalIgnoreCase))

                    // Audio & Video
                    || MediaPreviewViewModel.Extensions.Any((ext) => item.FileExtension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                    )).Count();

                int foldersCount = filesystemViewModel.FilesAndFolders.Where((item) => item.PrimaryItemAttribute == StorageItemTypes.Folder).Count();

                int otherFilesCount = filesystemViewModel.FilesAndFolders.Count - (imagesAndVideosCount + foldersCount);

                if (foldersCount > 0)
                { // There are folders in current directory

                    if ((filesystemViewModel.FilesAndFolders.Count - imagesAndVideosCount) < (filesystemViewModel.FilesAndFolders.Count - 20) || (filesystemViewModel.FilesAndFolders.Count <= 20 && imagesAndVideosCount >= 5))
                    { // Most of items are images/videos
                        folderSettings.ToggleLayoutModeTiles.Execute(false);
                    }
                    else
                    {
                        folderSettings.ToggleLayoutModeDetailsView.Execute(false);
                    }
                }
                else
                { // There are only files

                    if (imagesAndVideosCount == filesystemViewModel.FilesAndFolders.Count)
                    { // Only images/videos
                        preferredGridLayout.Execute(false);
                    }
                    else if (otherFilesCount < 20)
                    { // Most of files are images/videos
                        folderSettings.ToggleLayoutModeTiles.Execute(false);
                    }
                    else
                    { // Images/videos and other files
                        folderSettings.ToggleLayoutModeDetailsView.Execute(false);
                    }
                }

                return true;
            }

            return false;
        }
    }
}
