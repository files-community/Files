using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Files.Common;

namespace Files.Helpers
{
    public static class AdaptiveLayoutHelpers
    {
        public static async Task<bool> PredictLayoutMode(IShellPage associatedInstance)
        {
            if (App.AppSettings.AdaptiveLayoutEnabled)
            {
                bool desktopIniFound = false;

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
                                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeTiles.Execute(null);
                                        break;
                                    }

                                case "Pictures":
                                    {
                                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall.Execute(null);
                                        break;
                                    }

                                case "Music":
                                    {
                                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView.Execute(null);
                                        break;
                                    }

                                case "Videos":
                                    {
                                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall.Execute(null);
                                        break;
                                    }

                                default:
                                    {
                                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView.Execute(null);
                                        break;
                                    }
                            }

                            desktopIniFound = true;
                        }

                    }
                    response = null;
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
                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeTiles.Execute(null);
                    }
                    else
                    {
                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView.Execute(null);
                    }
                }
                else
                { // There are only files

                    if (imagesAndVideosCount == associatedInstance.FilesystemViewModel.FilesAndFolders.Count)
                    { // Only images/videos
                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall.Execute(null);
                    }
                    else if (otherFilesCount < 20)
                    { // Most of files are images/videos
                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeTiles.Execute(null);
                    }
                    else
                    { // Images/videos and other files
                        associatedInstance.InstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView.Execute(null);
                    }
                }

                return true;
            }

            return false;
        }
    }
}
