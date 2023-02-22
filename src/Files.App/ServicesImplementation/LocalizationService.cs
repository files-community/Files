using Files.App.Extensions;
using Files.Core.Services;

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
