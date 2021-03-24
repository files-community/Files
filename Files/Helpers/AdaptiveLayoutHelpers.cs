using Files.ViewModels;
using Files.ViewModels.Previews;
using System;
using System.Linq;
using Windows.Storage;

namespace Files.Helpers
{
    public static class AdaptiveLayoutHelpers
    {
        public static bool PredictLayoutMode(FolderSettingsViewModel folderSettings, ItemViewModel filesystemViewModel)
        {
            if (App.AppSettings.AreLayoutPreferencesPerFolder && App.AppSettings.AdaptiveLayoutEnabled && !folderSettings.LayoutPreference.IsAdaptiveLayoutOverridden)
            {
                bool desktopIniFound = false;

                string path = filesystemViewModel?.WorkingDirectory;

                if (string.IsNullOrWhiteSpace(path))
                {
                    return false;
                }

                var iniPath = System.IO.Path.Combine(path, "desktop.ini");
                var iniContents = NativeFileOperationsHelper.ReadStringFromFile(iniPath)?.Trim();
                if (!string.IsNullOrEmpty(iniContents))
                {
                    var parser = new IniParser.Parser.IniDataParser();
                    parser.Configuration.ThrowExceptionsOnError = false;
                    var data = parser.Parse(iniContents);
                    if (data != null)
                    {
                        var viewModeSection = data.Sections.FirstOrDefault(x => "ViewState".Equals(x.SectionName, StringComparison.OrdinalIgnoreCase));
                        if (viewModeSection != null)
                        {
                            var folderTypeKey = viewModeSection.Keys.FirstOrDefault(s => "FolderType".Equals(s.KeyName, StringComparison.OrdinalIgnoreCase));
                            if (folderTypeKey != null)
                            {
                                switch (folderTypeKey.Value)
                                {
                                    case "Documents":
                                        {
                                            folderSettings.ToggleLayoutModeTiles.Execute(false);
                                            break;
                                        }

                                    case "Pictures":
                                        {
                                            folderSettings.ToggleLayoutModeGridView.Execute(folderSettings.GridViewSize);
                                            break;
                                        }

                                    case "Music":
                                        {
                                            folderSettings.ToggleLayoutModeDetailsView.Execute(false);
                                            break;
                                        }

                                    case "Videos":
                                        {
                                            folderSettings.ToggleLayoutModeGridView.Execute(folderSettings.GridViewSize);
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
                        folderSettings.ToggleLayoutModeGridView.Execute(folderSettings.GridViewSize);
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