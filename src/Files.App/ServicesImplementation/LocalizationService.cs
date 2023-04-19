using Files.Backend.Services;

namespace Files.App.ServicesImplementation
{
	internal sealed class LocalizationService : ILocalizationService
	{
		public string LocalizeFromResourceKey(string resourceKey)
		{
			return resourceKey.GetLocalizedResource();
		}
	}
}
