using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared.Services;

namespace Files.Shared.Extensions
{
    public static class LocalizationExtensions
    {
        private static readonly ILocalizationService service
            = Ioc.Default.GetRequiredService<ILocalizationService>();

        public static string ToLocalized(this string resourceKey)
            => service.LocalizeFromResourceKey(resourceKey) ?? string.Empty;
    }
}
