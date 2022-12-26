using Files.Sdk.Storage.LocatableStorage;

namespace Files.Sdk.Storage.Extensions
{
	public static partial class StorageExtensions
	{
		public static string? TryGetPath(this IStorable storable)
		{
			return (storable as ILocatableStorable)?.Path;
		}
	}
}