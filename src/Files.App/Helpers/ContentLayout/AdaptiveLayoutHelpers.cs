// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Previews;
using IniParser.Model;
using Windows.Storage;
using static Files.App.Constants.AdaptiveLayout;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for content layout selection.
	/// </summary>
	internal static class AdaptiveLayoutHelpers
	{
		private static readonly IFoldersSettingsService foldersSettingsService =
			Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public static void ApplyAdaptativeLayout(FolderSettingsViewModel folderSettings, string path, IList<ListedItem> filesAndFolders)
		{
			if (foldersSettingsService.SyncFolderPreferencesAcrossDirectories ||
				string.IsNullOrWhiteSpace(path) ||
				folderSettings.IsLayoutModeFixed || !folderSettings.IsAdaptiveLayoutEnabled)
				return;

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

			return GetContentLayout(filesAndFolders);
		}

		private static LayoutTypes GetPathLayout(string path)
		{
			var iniPath = SystemIO.Path.Combine(path, "desktop.ini");

			var iniContents = NativeFileOperationsHelper.ReadStringFromFile(iniPath)?.Trim();
			if (string.IsNullOrEmpty(iniContents))
				return LayoutTypes.None;

			var parser = new IniParser.Parser.IniDataParser();
			parser.Configuration.ThrowExceptionsOnError = false;
			var data = parser.Parse(iniContents);
			if (data is null)
				return LayoutTypes.None;

			var viewModeSection = data.Sections.FirstOrDefault(IsViewState);
			if (viewModeSection is null)
				return LayoutTypes.None;

			var folderTypeKey = viewModeSection.Keys.FirstOrDefault(IsFolderType);
			if (folderTypeKey is null)
				return LayoutTypes.None;

			return folderTypeKey.Value switch
			{
				"Pictures" => LayoutTypes.GridMedium,
				"Videos" => LayoutTypes.GridMedium,
				_ => LayoutTypes.Details,
			};

			static bool IsViewState(SectionData data)
			{
				return "ViewState".Equals(data.SectionName, StringComparison.OrdinalIgnoreCase);
			}

			static bool IsFolderType(KeyData data)
			{
				return "FolderType".Equals(data.KeyName, StringComparison.OrdinalIgnoreCase);
			}
		}

		private static LayoutTypes GetContentLayout(IList<ListedItem> filesAndFolders)
		{
			int itemCount = filesAndFolders.Count;

			if (filesAndFolders.Count is 0)
				return LayoutTypes.None;

			float folderPercentage = 100f * filesAndFolders.Count(IsFolder) / itemCount;
			float imagePercentage = 100f * filesAndFolders.Count(IsImage) / itemCount;
			float mediaPercentage = 100f * filesAndFolders.Count(IsMedia) / itemCount;
			float miscPercentage = 100f - (folderPercentage + imagePercentage + mediaPercentage);

			if (folderPercentage + miscPercentage > LargeThreshold)
				return LayoutTypes.Details;
			if (imagePercentage > ExtraLargeThreshold)
				return LayoutTypes.GridMedium;
			if (imagePercentage <= MediumThreshold)
				return LayoutTypes.Details;
			if (100f - imagePercentage <= SmallThreshold)
				return LayoutTypes.Details;
			if (folderPercentage + miscPercentage <= ExtraSmallThreshold)
				return LayoutTypes.Details;
			return LayoutTypes.GridMedium;

			static bool IsFolder(ListedItem item)
			{
				return item.PrimaryItemAttribute is StorageItemTypes.Folder;
			}

			static bool IsImage(ListedItem item)
			{
				return
					!string.IsNullOrEmpty(item.FileExtension) &&
					ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant());
			}

			static bool IsMedia(ListedItem item)
			{
				return 
					!string.IsNullOrEmpty(item.FileExtension) &&
					MediaPreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant());
			}
		}
	}
}
