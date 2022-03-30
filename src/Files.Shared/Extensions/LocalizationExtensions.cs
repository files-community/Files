using System;

namespace Files.Shared.Extensions
{
    public static class LocalizationExtensions
    {
        private static bool isInitialized = false;

        private static Func<string, string> translate = (resourceKey) => resourceKey;

        public static void Initialize(Func<string, string> translate)
        {
            if (!isInitialized)
            {
                LocalizationExtensions.translate = translate;
                isInitialized = true;
            }
        }

        public static string GetLocalized(this string resourceKey)
            => translate(resourceKey) ?? string.Empty;
    }
}
