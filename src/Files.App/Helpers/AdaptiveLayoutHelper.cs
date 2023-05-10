// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Contexts;
using Files.App.ViewModels.Previews;
using Windows.Storage;

namespace Files.App.Helpers
{
	internal static class AdaptiveLayoutHelper
	{
		private static IFoldersSettingsService FoldersSettingsService { get; } = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		internal static void ApplyAdaptativeLayout(FolderSettingsViewModel folderSettings, string path, IList<ListedItem> filesAndFolders)
		{
			// Validating the value of each parameter
			if (FoldersSettingsService.SyncFolderPreferencesAcrossDirectories ||
				string.IsNullOrWhiteSpace(path) ||
				folderSettings.IsLayoutModeFixed ||
				!folderSettings.IsAdaptiveLayoutEnabled)
			{
				return;
			}

			// Change settings according to the page layout type of the given folder
			var layout = GetAdaptiveLayout(path, filesAndFolders);
			switch (layout)
			{
				case LayoutTypes.Details:
					folderSettings.ToggleLayoutModeDetailsView(false);
					break;
				case LayoutTypes.GridMedium:
					folderSettings.ToggleLayoutModeGridView(folderSettings.GridViewSize);
					break;
			}
		}

		private static LayoutTypes GetAdaptiveLayout(string path, IList<ListedItem> filesAndFolders)
		{
			var pathLayout = GetPathLayout(path);
			if (pathLayout is not LayoutTypes.None)
				return pathLayout;
			else
				return GetContentLayout(filesAndFolders);
		}

		private static LayoutTypes GetPathLayout(string path)
		{
			// Get desktop.ini in the given folder
			var iniPath = SystemIO.Path.Combine(path, "desktop.ini");

			// Get text from the desktop.ini
			var iniContents = NativeFileOperationsHelper.ReadStringFromFile(iniPath)?.Trim();
			if (string.IsNullOrEmpty(iniContents))
				return LayoutTypes.None;

			// Parse the INI file
			var parser = new IniParser.Parser.IniDataParser(new() { ThrowExceptionsOnError = false });
			var data = parser.Parse(iniContents);
			if (data is null)
				return LayoutTypes.None;

			// Get view state
			var viewModeSection = data.Sections.FirstOrDefault(x => x.SectionName.Equals("ViewState", StringComparison.OrdinalIgnoreCase));
			if (viewModeSection is null)
				return LayoutTypes.None;

			// Get folder style
			var folderTypeKey = viewModeSection.Keys.FirstOrDefault(x => x.KeyName.Equals("FolderType", StringComparison.OrdinalIgnoreCase));
			if (folderTypeKey is null)
				return LayoutTypes.None;

			// Determine layout type according to the folder tyle
			return folderTypeKey.Value switch
			{
				"Pictures" => LayoutTypes.GridMedium,
				"Videos" => LayoutTypes.GridMedium,
				_ => LayoutTypes.Details,
			};
		}

		private static LayoutTypes GetContentLayout(IList<ListedItem> filesAndFolders)
		{
			int itemCount = filesAndFolders.Count;
			if (filesAndFolders.Count is 0)
				return LayoutTypes.None;

			float folderPercentage = 100f * filesAndFolders.Count(x => x.PrimaryItemAttribute is StorageItemTypes.Folder) / itemCount;
			float imagePercentage = 100f * filesAndFolders.Count(x => string.IsNullOrEmpty(x.FileExtension) && ImagePreviewViewModel.ContainsExtension(x.FileExtension.ToLowerInvariant())) / itemCount;
			float mediaPercentage = 100f * filesAndFolders.Count(x => !string.IsNullOrEmpty(x.FileExtension) && MediaPreviewViewModel.ContainsExtension(x.FileExtension.ToLowerInvariant())) / itemCount;
			float miscPercentage = 100f - (folderPercentage + imagePercentage + mediaPercentage);

			if (folderPercentage + miscPercentage > Constants.AdaptiveLayout.LargeThreshold)
				return LayoutTypes.Details;
			else if (imagePercentage > Constants.AdaptiveLayout.ExtraLargeThreshold)
				return LayoutTypes.GridMedium;
			else if (imagePercentage <= Constants.AdaptiveLayout.MediumThreshold)
				return LayoutTypes.Details;
			else if (100f - imagePercentage <= Constants.AdaptiveLayout.SmallThreshold)
				return LayoutTypes.Details;
			else if (folderPercentage + miscPercentage <= Constants.AdaptiveLayout.ExtraSmallThreshold)
				return LayoutTypes.Details;
			else
				return LayoutTypes.GridMedium;
		}
	}
}
