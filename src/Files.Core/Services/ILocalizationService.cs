namespace Files.Core.Services
{
	public interface ILocalizationService
	{
		string LocalizeFromResourceKey(string resourceKey);
	}
}