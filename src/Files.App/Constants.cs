// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App
{
	public static class Constants
	{
		public static class AdaptiveLayout
		{
			public const float ExtraLargeThreshold = 85.0f;

			public const float LargeThreshold = 80.0f;

			public const float MediumThreshold = 60.0f;

			public const float SmallThreshold = 25.0f;

			public const float ExtraSmallThreshold = 15.0f;
		}

		public static class KnownImageFormats
		{
			public const string BITMAP_IMAGE_FORMAT = "bitmapimage";
		}

		public static class ImageRes
		{
			// See imageres.dll for more icon indexes to add
			public const int QuickAccess = 1024;
			public const int Desktop = 183;
			public const int Downloads = 184;
			public const int Documents = 112;
			public const int Pictures = 113;
			public const int Music = 108;
			public const int Videos = 189;
			public const int GenericDiskDrive = 35;
			public const int WindowsDrive = 36;
			public const int ThisPC = 109;
			public const int NetworkDrives = 25;
			public const int RecycleBin = 55;
			public const int CloudDrives = 1040;
			public const int OneDrive = 1043;
			public const int Libraries = 1023;
			public const int Folder = 3;
			public const int ShieldIcon = 78;
		}

		public static class Shell32
		{
			// See shell32.dll for more icon indexes to add
			public const int QuickAccess = 51380;
		}

		public static class FluentIconsPaths
		{
			public const string CloudDriveIcon = "ms-appx:///Assets/FluentIcons/CloudDrive.png";
			public const string FavoritesIcon = "ms-appx:///Assets/FluentIcons/Favorites.png";
			public const string FileTagsIcon = "ms-appx:///Assets/FluentIcons/FileTags.png";
			public const string HomeIcon = "ms-appx:///Assets/FluentIcons/Home.png";
		}

		public static class WslIconsPaths
		{
			public const string Alpine = "ms-appx:///Assets/WSL/alpinepng.png";
			public const string DebianIcon = "ms-appx:///Assets/WSL/debianpng.png";
			public const string GenericIcon = "ms-appx:///Assets/WSL/genericpng.png";
			public const string KaliIcon = "ms-appx:///Assets/WSL/kalipng.png";
			public const string OpenSuse = "ms-appx:///Assets/WSL/opensusepng.png";
			public const string UbuntuIcon = "ms-appx:///Assets/WSL/ubuntupng.png";
		}

		public static class AssetPaths
		{
			public const string DevLogo = "Assets/AppTiles/Dev/Logo.ico";
			public const string PreviewLogo = "Assets/AppTiles/Preview/Logo.ico";
			public const string StableLogo = "Assets/AppTiles/Release/Logo.ico";
		}

		public static class UI
		{
			public const float DimItemOpacity = 0.4f;

			/// <summary>
			/// The minimum width of the sidebar in expanded state
			/// </summary>
			public const double MinimumSidebarWidth = 180;

			public const double MaximumSidebarWidth = 500;

			// For contextmenu hacks, must match WinUI style
			public const double ContextMenuMaxHeight = 480;

			// For contextmenu hacks, must match WinUI style
			public const double ContextMenuSecondaryItemsHeight = 32;

			// For contextmenu hacks, must match WinUI style
			public const double ContextMenuPrimaryItemsHeight = 48;

			// For contextmenu hacks
			public const double ContextMenuLabelMargin = 10;

			// For contextmenu hacks
			public const double ContextMenuItemsMaxWidth = 250;
		}

		public static class Browser
		{
			public static class GridViewBrowser
			{
				public const int GridViewIncrement = 20;

				// Max achievable ctrl + scroll, not a default layout size
				public const int GridViewSizeMax = 300;

				public const int GridViewSizeLarge = 220;

				public const int GridViewSizeMedium = 160;

				public const int GridViewSizeSmall = 100;

				public const int TilesView = 260;
			}

			public static class DetailsLayoutBrowser
			{
				public const int DetailsViewSize = 32;
			}

			public static class ColumnViewBrowser
			{
				public const int ColumnViewSize = 32;

				public const int ColumnViewSizeSmall = 24;
			}
		}

		public static class Widgets
		{
			public static class Bundles
			{
				public const int MaxAmountOfItemsPerBundle = 8;
			}

			public static class Drives
			{
				public const float LowStorageSpacePercentageThreshold = 90.0f;
			}

			public const int WidgetIconSize = 256;
		}

		public static class LocalSettings
		{
			public const string DateTimeFormat = "datetimeformat";

			public const string Theme = "theme";

			public const string SettingsFolderName = "settings";

			public const string BundlesSettingsFileName = "bundles.json";

			public const string UserSettingsFileName = "user_settings.json";

			public const string FileTagSettingsFileName = "filetags.json";
		}

		public static class PreviewPane
		{
			/// <summary>
			/// The maximum number of characters that should be loaded into the preview.
			/// Enforcing this limit ensures that attempting to open an absurdly large file will not cause Files to freeze.
			/// </summary>
			public const int TextCharacterLimit = 10000;

			/// <summary>
			/// The maximum number of pages loaded into the PDF preview.
			/// </summary>
			public const int PDFPageLimit = 10;

			/// <summary>
			/// The maximum file size, in bytes, that will attempted to be loaded as text if the extension is unknown.
			/// </summary>
			public const long TryLoadAsTextSizeLimit = 500000;
		}

		public static class ResourceFilePaths
		{
			/// <summary>
			/// The path to the json file containing a list of file properties to be loaded in the properties window details page.
			/// </summary>
			public const string DetailsPagePropertiesJsonPath = @"ms-appx:///Resources/PropertiesInformation.json";

			/// <summary>
			/// The path to the json file containing a list of file properties to be loaded in the preview pane.
			/// </summary>
			public const string PreviewPaneDetailsPropertiesJsonPath = @"ms-appx:///Resources/PreviewPanePropertiesInformation.json";
		}

		public static class Filesystem
		{
			public const int ExtendedAsciiCodePage = 437;

			public const string CachedEmptyItemName = "fileicon_cache";
		}

		public static class GitHub
		{
			public const string GitHubRepoUrl = @"https://github.com/files-community/Files";
			public const string DocumentationUrl = @"https://files.community/docs";
			public const string FeatureRequestUrl = @"https://github.com/files-community/Files/issues/new?assignees=&labels=feature+request&template=feature_request.yml";
			public const string BugReportUrl = @"https://github.com/files-community/Files/issues/new?assignees=&labels=bug&template=bug_report.yml";
			public const string PrivacyPolicyUrl = @"https://github.com/files-community/Files/blob/main/Privacy.md";
			public const string SupportUsUrl = @"https://github.com/sponsors/yaira2";
		}

		public static class Actions
		{
			public const int MaxSelectedItems = 5;
		}
	}
}
