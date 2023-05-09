// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Previews;
using IniParser.Model;
using Windows.Storage;

namespace Files.App.Helpers
{
	internal static class AdaptiveLayoutHelpers
	{
		private static readonly IFoldersSettingsService foldersSettingsService = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		internal static void ApplyAdaptativeLayout(FolderSettingsViewModel folderSettings, string path, IList<ListedItem> filesAndFolders)
		{
			if (foldersSettingsService.SyncFolderPreferencesAcrossDirectories ||
				string.IsNullOrWhiteSpace(path) ||
				folderSettings.IsLayoutModeFixed ||
				!folderSettings.IsAdaptiveLayoutEnabled)
			{
				return;
			}

			var layout = GetAdaptiveLayout(path, filesAndFolders);
			switch (layout)
			{
				case Layouts.Detail:
					folderSettings.ToggleLayoutModeDetailsView(false);
					break;
				case Layouts.Grid:
					folderSettings.ToggleLayoutModeGridView(folderSettings.GridViewSize);
					break;
			}
		}

		private static Layouts GetAdaptiveLayout(string path, IList<ListedItem> filesAndFolders)
		{
			var pathLayout = GetPathLayout(path);
			if (pathLayout is not Layouts.None)
				return pathLayout;

			return GetContentLayout(filesAndFolders);
		}

		private static Layouts GetPathLayout(string path)
		{
			var iniPath = SystemIO.Path.Combine(path, "desktop.ini");

			var iniContents = NativeFileOperationsHelper.ReadStringFromFile(iniPath)?.Trim();
			if (string.IsNullOrEmpty(iniContents))
				return Layouts.None;

			var parser = new IniParser.Parser.IniDataParser();
			parser.Configuration.ThrowExceptionsOnError = false;
			var data = parser.Parse(iniContents);
			if (data is null)
				return Layouts.None;

			var viewModeSection = data.Sections.FirstOrDefault(IsViewState);
			if (viewModeSection is null)
				return Layouts.None;

			var folderTypeKey = viewModeSection.Keys.FirstOrDefault(IsFolderType);
			if (folderTypeKey is null)
				return Layouts.None;

			return folderTypeKey.Value switch
			{
				"Pictures" => Layouts.Grid,
				"Videos" => Layouts.Grid,
				_ => Layouts.Detail,
			};

			static bool IsViewState(SectionData data)
				=> "ViewState".Equals(data.SectionName, StringComparison.OrdinalIgnoreCase);

			static bool IsFolderType(KeyData data)
				=> "FolderType".Equals(data.KeyName, StringComparison.OrdinalIgnoreCase);
		}

		private static Layouts GetContentLayout(IList<ListedItem> filesAndFolders)
		{
			int itemCount = filesAndFolders.Count;
			if (filesAndFolders.Count is 0)
				return Layouts.None;

			float folderPercentage = 100f * filesAndFolders.Count(IsFolder) / itemCount;
			float imagePercentage = 100f * filesAndFolders.Count(IsImage) / itemCount;
			float mediaPercentage = 100f * filesAndFolders.Count(IsMedia) / itemCount;
			float miscPercentage = 100f - (folderPercentage + imagePercentage + mediaPercentage);

			if (folderPercentage + miscPercentage > Constants.AdaptiveLayout.LargeThreshold)
				return Layouts.Detail;
			if (imagePercentage > Constants.AdaptiveLayout.ExtraLargeThreshold)
				return Layouts.Grid;
			if (imagePercentage <= Constants.AdaptiveLayout.MediumThreshold)
				return Layouts.Detail;
			if (100f - imagePercentage <= Constants.AdaptiveLayout.SmallThreshold)
				return Layouts.Detail;
			if (folderPercentage + miscPercentage <= Constants.AdaptiveLayout.ExtraSmallThreshold)
				return Layouts.Detail;
			return Layouts.Grid;

			static bool IsFolder(ListedItem item)
				=> item.PrimaryItemAttribute is StorageItemTypes.Folder;

			static bool IsImage(ListedItem item)
				=> !string.IsNullOrEmpty(item.FileExtension)
				&& ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant());

			static bool IsMedia(ListedItem item)
				=> !string.IsNullOrEmpty(item.FileExtension)
				&& MediaPreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant());
		}

		private enum Layouts
		{
			None, // Don't decide. Another function to decide can be called afterwards if available.

			Detail, // Apply the layout Detail.

			Grid, // Apply the layout Grid.
		}
	}
}
