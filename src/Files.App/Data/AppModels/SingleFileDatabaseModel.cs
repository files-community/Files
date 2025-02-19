using Files.Core.Storage;
using Files.Core.Storage.Extensions;
using Files.Core.Storage.Storables;
using Files.Shared.Extensions;
using Files.Shared.Utils;
using System.IO;

namespace Files.App.Data.AppModels
{
	/// <inheritdoc cref="BaseDatabaseModel{TDictionaryValue}"/>
	public sealed partial class SingleFileDatabaseModel : BaseDatabaseModel<ISerializedModel>
	{
		private readonly string _fileName;
		private readonly IModifiableFolder _settingsFolder;
		private IFile? _databaseFile;

		public SingleFileDatabaseModel(string fileName, IModifiableFolder settingsFolder, IAsyncSerializer<Stream> serializer)
			: base(serializer)
		{
			_fileName = fileName;
			_settingsFolder = settingsFolder;
		}

		/// <inheritdoc/>
		public override TValue? GetValue<TValue>(string key, Func<TValue>? defaultValue = null)
			where TValue : default
		{
			if (settingsCache.TryGetValue(key, out var value))
				return value.GetValue<TValue?>() ?? (defaultValue is not null ? defaultValue() : default);

			var fallback = defaultValue is not null ? defaultValue() : default;
			settingsCache[key] = new NonSerializedData(fallback);

			return fallback;
		}

		/// <inheritdoc/>
		public override bool SetValue<TValue>(string key, TValue? value)
			where TValue : default
		{
			settingsCache[key] = new NonSerializedData(value);
			return true;
		}

		/// <inheritdoc/>
		public override async Task LoadAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				await storageSemaphore.WaitAsync(cancellationToken);
				await EnsureSettingsFileAsync(cancellationToken);

				_ = _databaseFile ?? throw new InvalidOperationException("The database file was not properly initialized.");

				await using var stream = await _databaseFile!.OpenStreamAsync(FileAccess.Read, FileShare.Read, cancellationToken);
				var settings = await serializer.DeserializeAsync<Stream, IDictionary>(stream, cancellationToken);

				// Reset the cache
				settingsCache.Clear();

				if (settings is null) // No settings saved, set cache to empty and return
					return;

				foreach (DictionaryEntry item in settings)
				{
					if (item.Key is not string key)
						continue;

					if (item.Value is ISerializedModel serializedData)
						settingsCache[key] = serializedData;
					else
						settingsCache[key] = new NonSerializedData(item.Value);
				}
			}
			finally
			{
				_ = storageSemaphore.Release();
			}
		}

		/// <inheritdoc/>
		public override async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				await storageSemaphore.WaitAsync(cancellationToken);
				await EnsureSettingsFileAsync(cancellationToken);

				_ = _databaseFile ?? throw new InvalidOperationException("The database file was not properly initialized.");

				await using var dataStream = await _databaseFile.OpenStreamAsync(FileAccess.ReadWrite, FileShare.Read, cancellationToken);
				await using var settingsStream = await serializer.SerializeAsync<Stream, IDictionary>(settingsCache, cancellationToken);

				// Overwrite existing content
				dataStream.Position = 0L;
				dataStream.SetLength(0L);

				// Copy contents
				settingsStream.Position = 0L;
				await settingsStream.CopyToAsync(dataStream, cancellationToken);

				return true;
			}
			finally
			{
				_ = storageSemaphore.Release();
			}
		}

		private async Task<bool> EnsureSettingsFileAsync(CancellationToken cancellationToken)
		{
			if (_databaseFile is null)
				_databaseFile = await _settingsFolder.TryCreateFileAsync(_fileName, false, cancellationToken);

			return _databaseFile is not null;
		}

		/// <inheritdoc cref="ISerializedModel"/>
		private sealed class NonSerializedData : ISerializedModel
		{
			private readonly object? _value;

			public NonSerializedData(object? value)
			{
				_value = value;
			}

			/// <inheritdoc/>
			public T? GetValue<T>()
			{
				return _value.TryCast<T>();
			}
		}
	}
}
