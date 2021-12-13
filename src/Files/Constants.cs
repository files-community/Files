namespace Files
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

        public static class CommonPaths
        {
            public const string RecycleBinPath = @"Shell:RecycleBinFolder";

            public const string NetworkFolderPath = @"Shell:NetworkPlacesFolder";

            public const string MyComputerPath = @"Shell:MyComputerFolder";
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
        }

        public static class Shell32
        {
            // See shell32.dll for more icon indexes to add
            public const int QuickAccess = 51380;
        }

        public static class UI
        {
            public const float DimItemOpacity = 0.4f;

            /// <summary>
            /// The minimum width of the sidebar in expanded state
            /// </summary>
            public const double MinimumSidebarWidth = 250;

            public const double MaximumSidebarWidth = 500;

            public const double ContextMenuMaxHeight = 480; // For contextmenu hacks, must match WinUI style
            public const double ContextMenuSecondaryItemsHeight = 32; // For contextmenu hacks, must match WinUI style
            public const double ContextMenuPrimaryItemsHeight = 48; // For contextmenu hacks, must match WinUI style
            public const double ContextMenuLabelMargin = 10; // For contextmenu hacks
            public const double ContextMenuItemsMaxWidth = 250; // For contextmenu hacks
        }

        public static class Browser
        {
            public static class GridViewBrowser
            {
                public const int GridViewIncrement = 20;

                public const int GridViewSizeMax = 300; // Max achievable ctrl + scroll, not a default layout size.

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

            /// <summary>
            /// The number of thumbnails that will be shown for FolderPreviews
            /// </summary>
            public const int FolderPreviewThumbnailCount = 10;
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

        public static class OptionalPackages
        {
            public const string ThemesOptionalPackagesName = "49306atecsolution.ThemesforFiles";
        }

        public static class Filesystem
        {
            public const int ExtendedAsciiCodePage = 437;

            public const string CachedEmptyItemName = "fileicon_cache";
        }
    }
}