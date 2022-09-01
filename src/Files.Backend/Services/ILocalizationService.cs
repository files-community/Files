namespace Files.Backend.Services
{
    public interface ILocalizationService
    {
        string LocalizeFromResourceKey(string resourceKey);
    }
}