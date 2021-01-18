namespace Files
{
    public static class Constants
    {
        public static class Browser
        {
            public static class GridViewBrowser
            {
                public const int GridViewIncrement = 20;

                public const int GridViewSizeMax = 300; // Max achievable ctrl + scroll, not a default layout size.

                public const int GridViewSizeLarge = 220;

                public const int GridViewSizeMedium = 160;

                public const int GridViewSizeSmall = 100;
            }

            public static class GenericFileBrowser
            {
            }
        }

        public static class LocalSettings
        {
            public const string DateTimeFormat = "datetimeformat";
            public const string Theme = "theme";
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
            public const int PDFPageLimit = 50;

            /// <summary>
            /// If the file size is greater than this number, the file buffer will not be sent to the extension
            /// </summary>
            public const long MaxFileSizeToSendToExtensionBytes = 10000000;
        }
    }
}