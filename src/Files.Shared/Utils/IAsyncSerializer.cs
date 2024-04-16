using System;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Shared.Utils
{
	/// <summary>
	/// Provides data serialization abstractions for <typeparamref name="TSerialized"/> data.
	/// </summary>
	/// <typeparam name="TSerialized">The type of data serialized to.</typeparam>
	public interface IAsyncSerializer<TSerialized>
	{
		/// <summary>
		/// Serializes <paramref name="data"/> into <typeparamref name="TSerialized"/>.
		/// </summary>
		/// <param name="data">The data to serialize.</param>
		/// <param name="dataType">The type of data to serialize.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. Value is <typeparamref name="TSerialized"/> of transformed <paramref name="data"/>.</returns>
		Task<TSerialized> SerializeAsync(object? data, Type dataType, CancellationToken cancellationToken = default);

		/// <summary>
		/// Deserializes <paramref name="serialized"/> into <paramref name="dataType"/> type.
		/// </summary>
		/// <param name="serialized">The data to deserialize.</param>
		/// <param name="dataType">The type to deserialize into.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. Value is of type <paramref name="dataType"/> of transformed <paramref name="serialized"/>.</returns>
		Task<object?> DeserializeAsync(TSerialized serialized, Type dataType, CancellationToken cancellationToken = default);
	}
}
