namespace Files
{
    public static class Constants
    {
        public static class AdaptiveLayout
        {
            public static class FoldersAndGenericFiles
            {
                public const float ItemsThreshold = 80.0f;

                public const int DetailsAfterItemsAmount = 6; 
            }

            public static class Images
            {
                public const float FirstOrCond_ImagesThreshold = 85.0f;

                public const float SecondOrCondFirstAndCond_ImagesThreshold = 60.0f;

                public const float SecondOrCondSecondAndCond_MediaAndMiscAndFoldersSumThreshold = 25.0f;

                public const float SecondOrCondThirdAndCond_MiscAndFolderSumThreshold = 15.0f;
            }

            public static class Media
            {
                public const float FirstOrCond_MediaThreshold = 85.0f;

                public const float SecondOrCondFirstAndCond_MediaThreshold = 60.0f;

                public const float SecondOrCondSecondAndCond_ImagesAndMiscAndFoldersSumThreshold = 25.0f;

                public const float SecondOrCondThirdAndCond_MiscAndFoldersSumThreshold = 15.0f;

                public const float DetailsAfterItemsAmount = 16;
            }

            public static class Tiles
            {
                public const int AllTilesFromMediaAmountThreshold = 6;
                public const int AllTilesFromFoldersAndMiscAmountThreshold = 16;
            }
        }

        public static class UI
        {
            public const float DimItemOpacity = 0.4f;
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
                public const int DetailsViewSize = 28;
            }

            public static class ColumnViewBrowser
            {
                public const int ColumnViewSize = 28;
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
        }

        public static class PreviewPane
        {
            /// <summary>
            /// The maximum number of characters that should be loaded into the preview.
            /// Enforcing this limit ensures that attempting to open an absurdly large file will not cause Files to freeze.
            /// </summary>
            public const int TextCharacterLimit = 50000;

            /// <summary>
            /// The maximum number of pages loaded into the PDF preview.
            /// </summary>
            public const int PDFPageLimit = 10;

            /// <summary>
            /// The maximum file size, in bytes, that will attempted to be loaded as text if the extension is unknown.
            /// </summary>
            public const long TryLoadAsTextSizeLimit = 1000000;

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
    }
}