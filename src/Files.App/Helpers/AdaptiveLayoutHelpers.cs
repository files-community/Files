// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Previews;
using IniParser.Model;
using Windows.Storage;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper to handle content page layout.
	/// </summary>
	public static class AdaptiveLayoutHelpers
	{
		private static IFoldersSettingsService FoldersSettingsService { get; } = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public static void ApplyAdaptativeLayout(FolderSettingsViewModel folderSettings, string path, IList<ListedItem> filesAndFolders)
		{
			if (FoldersSettingsService.SyncFolderPreferencesAcrossDirectories)
				return;
			if (string.IsNullOrWhiteSpace(path))
				return;
			if (folderSettings.IsLayoutModeFixed || !folderSettings.IsAdaptiveLayoutEnabled)
				return;

			var layout = GetAdaptiveLayout(path, filesAndFolders);
			switch (layout)
			{
				case PageLayoutType.Detail:
					folderSettings.ToggleLayoutModeDetailsView(false);
					break;
				case PageLayoutType.Grid:
					folderSettings.ToggleLayoutModeGridView(folderSettings.GridViewSize);
					break;
			}
		}

		private static PageLayoutType GetAdaptiveLayout(string path, IList<ListedItem> filesAndFolders)
		{
			var pathLayout = GetPathLayout(path);
			if (pathLayout is not PageLayoutType.None)
				return pathLayout;

			return GetContentLayout(filesAndFolders);
		}

		private static PageLayoutType GetPathLayout(string path)
		{
			var iniPath = SystemIO.Path.Combine(path, "desktop.ini");

			var iniContents = NativeFileOperationsHelper.ReadStringFromFile(iniPath)?.Trim();
			if (string.IsNullOrEmpty(iniContents))
				return PageLayoutType.None;

			var parser = new IniParser.Parser.IniDataParser();
			parser.Configuration.ThrowExceptionsOnError = false;
			var data = parser.Parse(iniContents);
			if (data is null)
				return PageLayoutType.None;

			var viewModeSection = data.Sections.FirstOrDefault(IsViewState);
			if (viewModeSection is null)
				return PageLayoutType.None;

			var folderTypeKey = viewModeSection.Keys.FirstOrDefault(IsFolderType);
			if (folderTypeKey is null)
				return PageLayoutType.None;

			return folderTypeKey.Value switch
			{
				"Pictures" => PageLayoutType.Grid,
				"Videos" => PageLayoutType.Grid,
				_ => PageLayoutType.Detail,
			};

			static bool IsViewState(SectionData data)
				=> "ViewState".Equals(data.SectionName, StringComparison.OrdinalIgnoreCase);

			static bool IsFolderType(KeyData data)
				=> "FolderType".Equals(data.KeyName, StringComparison.OrdinalIgnoreCase);
		}

		private static PageLayoutType GetContentLayout(IList<ListedItem> filesAndFolders)
		{
			int itemCount = filesAndFolders.Count;
			if (filesAndFolders.Count is 0)
				return PageLayoutType.None;

			float folderPercentage = 100f * filesAndFolders.Count(IsFolder) / itemCount;
			float imagePercentage = 100f * filesAndFolders.Count(IsImage) / itemCount;
			float mediaPercentage = 100f * filesAndFolders.Count(IsMedia) / itemCount;
			float miscPercentage = 100f - (folderPercentage + imagePercentage + mediaPercentage);

			if (folderPercentage + miscPercentage > Constants.AdaptiveLayout.LargeThreshold)
				return PageLayoutType.Detail;
			if (imagePercentage > Constants.AdaptiveLayout.ExtraLargeThreshold)
				return PageLayoutType.Grid;
			if (imagePercentage <= Constants.AdaptiveLayout.MediumThreshold)
				return PageLayoutType.Detail;
			if (100f - imagePercentage <= Constants.AdaptiveLayout.SmallThreshold)
				return PageLayoutType.Detail;
			if (folderPercentage + miscPercentage <= Constants.AdaptiveLayout.ExtraSmallThreshold)
				return PageLayoutType.Detail;

			return PageLayoutType.Grid;

			static bool IsFolder(ListedItem item) =>
				item.PrimaryItemAttribute is StorageItemTypes.Folder;

			static bool IsImage(ListedItem item) =>
				!string.IsNullOrEmpty(item.FileExtension) &&
				ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant());

			static bool IsMedia(ListedItem item) =>
				!string.IsNullOrEmpty(item.FileExtension) &&
				MediaPreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant());
		}
	}
}
