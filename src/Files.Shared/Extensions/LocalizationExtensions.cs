using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared.Services;

namespace Files.Shared.Extensions
{
    public static class LocalizationExtensions
    {
        private static ILocalizationService? FallbackLocalizationService;

        public static string ToLocalized(this string resourceKey, ILocalizationService? localizationService = null)
        {
            if (localizationService is not null)
            {
                return localizationService.LocalizeFromResourceKey(resourceKey);
            }

            FallbackLocalizationService ??= Ioc.Default.GetService<ILocalizationService>();
            return FallbackLocalizationService?.LocalizeFromResourceKey(resourceKey) ?? string.Empty;
        }
    }
}
