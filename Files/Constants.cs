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

        public static class Widgets
        {
            public static class Bundles
            {
                public const int MaxAmountOfItemsPerBundle = 8;
            }
        }

        public static class LocalSettings
        {
            public const string DateTimeFormat = "datetimeformat";

            public const string Theme = "theme";

            public const string JsonSettingsFileName = "settingsjson.json";
        }
    }
}