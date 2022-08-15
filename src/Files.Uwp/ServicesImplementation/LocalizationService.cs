using Files.Backend.Services;
using CommunityToolkit.WinUI;

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
