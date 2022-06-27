using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Uwp.ViewModels;
using Files.Uwp.ViewModels.Previews;
using System;
using System.Linq;
using Windows.Storage;
using System.Collections.Generic;
using Files.Uwp.Filesystem;

namespace Files.Uwp.Helpers
{
    public static class AdaptiveLayoutHelpers
    {
        public static bool PredictLayoutMode(FolderSettingsViewModel folderSettings, string path, IList<ListedItem> filesAndFolders)
        {
            IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();

            if (userSettingsService.PreferencesSettingsService.AreLayoutPreferencesPerFolder
                && folderSettings.IsAdaptiveLayoutEnabled
                && !folderSettings.IsLayoutModeFixed)
            {
                Action layoutDetails = () => folderSettings.ToggleLayoutModeDetailsView(false);
                Action layoutTiles = () => folderSettings.ToggleLayoutModeTiles(false);
                Action layoutGridView = () => folderSettings.ToggleLayoutModeGridView(folderSettings.GridViewSize);

                bool desktopIniFound = false;

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
                                var setLayout = (folderTypeKey.Value) switch
                                {
                                    "Documents" => layoutDetails,
                                    "Pictures" => layoutGridView,
                                    "Music" => layoutDetails,
                                    "Videos" => layoutGridView,
                                    _ => layoutDetails
                                };
                                setLayout();
                                desktopIniFound = true;
                            }
                        }
                    }
                }

                if (desktopIniFound)
                {
                    return true;
                }
                if (filesAndFolders.Count == 0)
                {
                    return false;
                }

                int allItemsCount = filesAndFolders.Count;

                int mediaCount;
                int imagesCount;
                int foldersCount;
                int miscFilesCount;

                float mediaPercentage;
                float imagesPercentage;
                float foldersPercentage;
                float miscFilesPercentage;

                mediaCount = filesAndFolders.Where((item) =>
                {
                    return !string.IsNullOrEmpty(item.FileExtension) && MediaPreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant());
                }).Count();
                imagesCount = filesAndFolders.Where((item) =>
                {
                    return !string.IsNullOrEmpty(item.FileExtension) && ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant());
                }).Count();
                foldersCount = filesAndFolders.Where((item) => item.PrimaryItemAttribute == StorageItemTypes.Folder).Count();
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