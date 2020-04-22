using Windows.ApplicationModel.Resources;

namespace Files
{
    public static class ResourceController
    {
        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

        public static string GetTranslation(string resource)
        {
            return _resourceLoader.GetString(resource);
        }
    }
}