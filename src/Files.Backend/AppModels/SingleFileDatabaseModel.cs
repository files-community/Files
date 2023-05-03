using Files.Backend.Models;
using Files.Sdk.Storage.ModifiableStorage;
using Files.Sdk.Storage;
using Files.Shared.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Files.Shared;
using System.Threading.Tasks;
using Files.Sdk.Storage.Extensions;

namespace Files.Backend.AppModels
{
	/// <inheritdoc cref="BaseDatabaseModel{TDictionaryValue}"/>
	public sealed class SingleFileDatabaseModel : BaseDatabaseModel<ISerializedModel>
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
		public override async Task<bool> LoadAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				await storageSemaphore.WaitAsync(cancellationToken);
				if (!await EnsureSettingsFileAsync(cancellationToken))
					return false;

				_ = _databaseFile!;

				await using var stream = await _databaseFile!.TryOpenStreamAsync(FileAccess.Read, FileShare.Read, cancellationToken);
				if (stream is null)
					return false;

				var settings = await serializer.DeserializeAsync<Stream, IDictionary>(stream, cancellationToken);
				settingsCache.Clear();

				if (settings is null) // No settings saved, set cache to empty and return true.
					return true;

				foreach (DictionaryEntry item in settings)
				{
					if (item.Key is not string key)
						continue;

					if (item.Value is ISerializedModel serializedData)
						settingsCache[key] = serializedData;
					else
						settingsCache[key] = new NonSerializedData(item.Value);
				}

				return true;
			}
			catch (Exception)
			{
				return false;
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
				if (!await EnsureSettingsFileAsync(cancellationToken))
					return false;

				_ = _databaseFile!;

				await using var dataStream = await _databaseFile!.TryOpenStreamAsync(FileAccess.ReadWrite, FileShare.Read, cancellationToken);
				if (dataStream is null)
					return false;

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
			if (_databaseFile is not null)
				return true;

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
