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
                Action layoutDetails = () => folderSettings.ToggleLayoutModeDetailsView.Execute(false);
                Action layoutTiles = () => folderSettings.ToggleLayoutModeTiles.Execute(false);
                Action layoutGridView = () => folderSettings.ToggleLayoutModeGridView.Execute(folderSettings.GridViewSize);

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
                                            layoutDetails();
                                            break;
                                        }

                                    case "Pictures":
                                        {
                                            layoutGridView();
                                            break;
                                        }

                                    case "Music":
                                        {
                                            layoutDetails();
                                            break;
                                        }

                                    case "Videos":
                                        {
                                            layoutGridView();
                                            break;
                                        }

                                    default:
                                        {
                                            layoutDetails();
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

                int allItemsCount = filesystemViewModel.FilesAndFolders.Count;

                int mediaCount;
                int imagesCount;
                int foldersCount;
                int miscFilesCount;

                float mediaPercentage;
                float imagesPercentage;
                float foldersPercentage;
                float miscFilesPercentage;

                mediaCount = filesystemViewModel.FilesAndFolders.Where((item) =>
                {
                    return !string.IsNullOrEmpty(item.FileExtension) && MediaPreviewViewModel.Extensions.Any((ext) => item.FileExtension.Equals(ext, StringComparison.OrdinalIgnoreCase));
                }).Count();
                imagesCount = filesystemViewModel.FilesAndFolders.Where((item) =>
                {
                    return !string.IsNullOrEmpty(item.FileExtension) && ImagePreviewViewModel.Extensions.Any((ext) => item.FileExtension.Equals(ext, StringComparison.OrdinalIgnoreCase));
                }).Count();
                foldersCount = filesystemViewModel.FilesAndFolders.Where((item) => item.PrimaryItemAttribute == StorageItemTypes.Folder).Count();
                miscFilesCount = allItemsCount - (mediaCount + imagesCount + foldersCount);

                mediaPercentage = (float)((float)mediaCount / (float)allItemsCount) * 100.0f;
                imagesPercentage = (float)((float)imagesCount / (float)allItemsCount) * 100.0f;
                foldersPercentage = (float)((float)foldersCount / (float)allItemsCount) * 100.0f;
                miscFilesPercentage = (float)((float)miscFilesCount / (float)allItemsCount) * 100.0f;

                // Decide layout mode

                // Mostly files + folders, lesser media and image files | Mostly folders
                if ((foldersPercentage + miscFilesPercentage) > Constants.AdaptiveLayout.LargeThreshold)
                {
                    layoutDetails();
                }
                // Mostly images, probably an images folder
                else if (imagesPercentage > Constants.AdaptiveLayout.ExtraLargeThreshold
                    || (imagesPercentage > Constants.AdaptiveLayout.MediumThreshold
                        && (mediaPercentage + miscFilesPercentage + foldersPercentage) > Constants.AdaptiveLayout.SmallThreshold
                        && (miscFilesPercentage + foldersPercentage) < Constants.AdaptiveLayout.ExtraSmallThreshold))
                {
                    layoutGridView();
                }
                // Mostly media i.e. sound files, videos
                else if (mediaPercentage > Constants.AdaptiveLayout.ExtraLargeThreshold
                    || (mediaPercentage > Constants.AdaptiveLayout.MediumThreshold
                    && (imagesPercentage + miscFilesPercentage + foldersPercentage) > Constants.AdaptiveLayout.SmallThreshold
                    && (miscFilesPercentage + foldersPercentage) < Constants.AdaptiveLayout.ExtraSmallThreshold))
                {
                    layoutDetails();
                }
                else
                {
                    layoutDetails();
                }

                return true;
            }

            return false;
        }
    }
}