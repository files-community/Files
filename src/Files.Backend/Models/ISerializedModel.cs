namespace Files.Backend.Models
{
	/// <summary>
	/// Represents a model that holds serialized data.
	/// </summary>
	public interface ISerializedModel
	{
		/// <summary>
		/// Retrieves requested <typeparamref name="T"/> of the serialized data.
		/// </summary>
		/// <typeparam name="T">The type of data to retrieve.</typeparam>
		/// <returns>Deserialized data of type <typeparamref name="T"/>. If the value cannot be deserialized, returns default.</returns>
		T? GetValue<T>();
	}
}
