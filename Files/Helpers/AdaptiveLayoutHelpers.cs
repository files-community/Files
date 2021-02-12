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

namespace Files.Helpers
{
    public static class AdaptiveLayoutHelpers
    {
        public static void SetPreferredLayout(string layoutName)
        {
            string rawPreferredLayoutMode = App.AppSettings.AdaptiveLayoutPreferredLayoutMode;

            if (string.IsNullOrWhiteSpace(rawPreferredLayoutMode) || rawPreferredLayoutMode == "null")
            {
                App.AppSettings.AdaptiveLayoutPreferredLayoutMode = layoutName;

                return;
            }
            // else

            string serialized = string.Empty;

            foreach (string item in rawPreferredLayoutMode.Split('|'))
            {
                string itemToAdd = item;

                if ((item == "GridViewSmall" || item == "GridViewMedium" || item == "GridViewLarge")
                    && (layoutName == "GridViewSmall" || layoutName == "GridViewMedium" || layoutName == "GridViewLarge"))
                {
                    itemToAdd = layoutName;
                }

                serialized += $"{itemToAdd}|";
            }

            if (serialized.EndsWith('|'))
            {
                serialized = serialized.TrimEnd('|');
            }

            App.AppSettings.AdaptiveLayoutPreferredLayoutMode = serialized;
        }

        public static List<string> GetPreferredLayouts()
        {
            string rawPreferredLayoutMode = App.AppSettings.AdaptiveLayoutPreferredLayoutMode;

            List<string> preferredLayouts = new List<string>();
            if (!string.IsNullOrWhiteSpace(rawPreferredLayoutMode) && rawPreferredLayoutMode != "null")
            {
                preferredLayouts = rawPreferredLayoutMode.Split('|').ToList();

                return preferredLayouts;
            }

            return preferredLayouts;
        }

        public static async Task<bool> PredictLayoutMode(IShellPage associatedInstance)
        {
            if (App.AppSettings.AdaptiveLayoutEnabled)
            {
                bool desktopIniFound = false;
                List<string> preferredLayouts = GetPreferredLayouts();

                ICommand preferredGridLayout = associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall; // Default

                foreach (string item in preferredLayouts)
                {
                    switch (item)
                    {
                        case "GridViewSmall":
                            preferredGridLayout = associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall;
                            break;

                        case "GridViewMedium":
                            preferredGridLayout = associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewMedium;
                            break;

                        case "GridViewLarge":
                            preferredGridLayout = associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewLarge;
                            break;
                    }
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

                    && (item.FileExtension.Equals(".svg", StringComparison.OrdinalIgnoreCase)
                    || item.FileExtension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                    || item.FileExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                    || item.FileExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)

                    || item.FileExtension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)
                    || item.FileExtension.Equals(".mkv", StringComparison.OrdinalIgnoreCase)
                    || item.FileExtension.Equals(".webm", StringComparison.OrdinalIgnoreCase)
                    || item.FileExtension.Equals(".ogg", StringComparison.OrdinalIgnoreCase)
                    || item.FileExtension.Equals(".qt", StringComparison.OrdinalIgnoreCase)

                    || item.FileExtension.Equals(".gif", StringComparison.OrdinalIgnoreCase))).Count();

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
