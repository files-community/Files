namespace Files.Shared.Services
{
    public interface ILocalizationService
    {
        string LocalizeFromResourceKey(string resourceKey);
    }
}