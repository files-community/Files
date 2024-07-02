// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Previews;
using Files.Shared.Helpers;
using Windows.Storage;
using static Files.App.Constants.AdaptiveLayout;

namespace Files.App.Helpers
{
	// TODO: Move this to ShellViewModel
	public static class AdaptiveLayoutHelpers
	{
		private static ILayoutSettingsService LayoutSettingsService { get; } = Ioc.Default.GetRequiredService<ILayoutSettingsService>();
		private static IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public static void ApplyAdaptativeLayout(LayoutPreferencesManager folderSettings, IList<ListedItem> filesAndFolders)
		{
			if (LayoutSettingsService.SyncFolderPreferencesAcrossDirectories ||
				folderSettings.IsLayoutModeFixed ||
				!folderSettings.IsAdaptiveLayoutEnabled)
				return;

			Layouts layout = Layouts.None;

			// Get layout from 'desktop.INI' file
			var viewStateSection = ContentPageContext.ShellPage!.ShellViewModel.DesktopIni.FirstOrDefault(x => x.SectionName == "ViewState");
			if (viewStateSection is not null)
			{
				var viewMode = viewStateSection.Parameters?.FirstOrDefault(x => x.Key == "Mode").Value;

				layout = viewMode switch
				{
					"Pictures" => Layouts.Grid,
					"Videos" => Layouts.Grid,
					_ => Layouts.Detail,
				};
			}

			// Calculate layout from contents
			if (layout is Layouts.None)
			{
				if (filesAndFolders.Count is 0)
					layout = Layouts.None;

				float folderPercentage = 100f * filesAndFolders.Count(IsFolder) / filesAndFolders.Count;
				float imagePercentage = 100f * filesAndFolders.Count(IsImage) / filesAndFolders.Count;
				float mediaPercentage = 100f * filesAndFolders.Count(IsMedia) / filesAndFolders.Count;
				float miscPercentage = 100f - (folderPercentage + imagePercentage + mediaPercentage);

				if (folderPercentage + miscPercentage > LargeThreshold)
					layout = Layouts.Detail;
				else if (imagePercentage > ExtraLargeThreshold)
					layout = Layouts.Grid;
				else if (imagePercentage <= MediumThreshold)
					layout = Layouts.Detail;
				else if (100f - imagePercentage <= SmallThreshold)
					layout = Layouts.Detail;
				else if (folderPercentage + miscPercentage <= ExtraSmallThreshold)
					layout = Layouts.Detail;
				else
					layout = Layouts.Grid;
			}

			switch (layout)
			{
				case Layouts.Detail:
					folderSettings.ToggleLayoutModeDetailsView(false);
					break;
				case Layouts.Grid:
					folderSettings.ToggleLayoutModeGridView(false);
					break;
			}

			static bool IsFolder(ListedItem item)
				=> item.PrimaryItemAttribute is StorageItemTypes.Folder;

			static bool IsImage(ListedItem item) =>
				!string.IsNullOrEmpty(item.FileExtension) &&
				ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant());

			static bool IsMedia(ListedItem item) =>
				!string.IsNullOrEmpty(item.FileExtension) &&
				(FileExtensionHelpers.IsAudioFile(item.FileExtension) ||
				FileExtensionHelpers.IsVideoFile(item.FileExtension));
		}

		private enum Layouts
		{
			None, // Don't decide. Another function to decide can be called afterwards if available.
			Detail, // Apply the layout Detail.
			Grid, // Apply the layout Grid.
		}
	}
}
