using Files.Backend.Services;
using Files.Uwp.Extensions;

namespace Files.Uwp.ServicesImplementation
{
    internal sealed class LocalizationService : ILocalizationService
    {
        public string LocalizeFromResourceKey(string resourceKey)
        {
            return resourceKey.GetLocalizedResource();
        }
    }
}
