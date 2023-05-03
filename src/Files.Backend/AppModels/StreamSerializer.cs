using Files.Shared.Utils;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.AppModels
{
	/// <summary>
	/// Implementation for <see cref="IAsyncSerializer{TSerialized}"/> that uses <see cref="Stream"/> to serialize/deserialize JSON.
	/// </summary>
	public class StreamSerializer : IAsyncSerializer<Stream>
	{
		private JsonSerializerOptions DefaultSerializerOptions { get; }

		/// <summary>
		/// A single instance of <see cref="StreamSerializer"/>.
		/// </summary>
		public static StreamSerializer Instance { get; } = new();

		protected StreamSerializer()
		{
			DefaultSerializerOptions = new()
			{
				WriteIndented = true
			};
		}

		/// <inheritdoc/>
		public virtual async Task<Stream> SerializeAsync(object? data, Type dataType, CancellationToken cancellationToken = default)
		{
			var outputStream = new MemoryStream();

			// Serialize data to stream
			await JsonSerializer.SerializeAsync(outputStream, data, dataType, DefaultSerializerOptions, cancellationToken);
			outputStream.Position = 0;

			return outputStream;
		}

		/// <inheritdoc/>
		public virtual async Task<object?> DeserializeAsync(Stream serialized, Type dataType, CancellationToken cancellationToken = default)
		{
			if (serialized.CanSeek)
				serialized.Position = 0L;

			var deserialized = await JsonSerializer.DeserializeAsync(serialized, dataType, DefaultSerializerOptions, cancellationToken);
			return deserialized;
		}
	}
}
