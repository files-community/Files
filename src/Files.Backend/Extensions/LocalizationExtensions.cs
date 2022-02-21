using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services;

#nullable enable

namespace Files.Backend.Extensions
{
    public static class LocalizationExtensions
    {
        private static ILocalizationService? FallbackLocalizationService;

        public static string GetLocalized(this string resourceKey, ILocalizationService? localizationService = null)
        {
            if (localizationService == null)
            {
                FallbackLocalizationService ??= Ioc.Default.GetRequiredService<ILocalizationService>();
                return FallbackLocalizationService.LocalizeFromResourceKey(resourceKey);
            }

            return localizationService.LocalizeFromResourceKey(resourceKey);
        }
    }
}
