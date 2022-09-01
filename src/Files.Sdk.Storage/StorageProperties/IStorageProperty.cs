using System.Threading.Tasks;

namespace Files.Sdk.Storage.StorageProperties
{
	public interface IStorageProperty
	{
		string Name { get; }

		object? Value { get; }

		Task<bool> ModifyAsync(object newValue);
	}
}
