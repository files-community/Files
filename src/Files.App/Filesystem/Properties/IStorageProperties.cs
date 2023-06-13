namespace Files.App.Filesystem.Properties
{
	public interface IStorageProperties
	{
		DateTime DateCreated { get; }

		DateTime DateModified { get; }

		ulong? Size { get; }

		Task<KeyValuePair<string, object>> GetStoragePropertyAsync(string canonicalName);

		IAsyncEnumerable<KeyValuePair<string, object>> GetStoragePropertiesAsync(ItemPropertiesKind propertiesKind = ItemPropertiesKind.Standard, CancellationToken cancellationToken = default);
		
		IAsyncEnumerable<KeyValuePair<string, object>> RetrievePropertiesAsync(string[] strings);
	}
}
