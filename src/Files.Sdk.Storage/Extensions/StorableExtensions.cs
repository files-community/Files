using Files.Sdk.Storage.LocatableStorage;

namespace Files.Sdk.Storage.Extensions
{
    public static class StorableExtensions
    {
        public static string? TryGetPath(this IStorable storable)
        {
            return (storable as ILocatableStorable)?.Path;
        }
    }
}
