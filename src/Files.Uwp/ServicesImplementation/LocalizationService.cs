using Files.Backend.Services;
using Microsoft.Toolkit.Uwp;

namespace Files.Uwp.ServicesImplementation
{
    internal sealed class LocalizationService : ILocalizationService
    {
        public string LocalizeFromResourceKey(string resourceKey)
        {
            return resourceKey.GetLocalized();
        }
    }
}
