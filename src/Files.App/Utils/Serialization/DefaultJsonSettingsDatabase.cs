// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;
using System.Text.Json;
using Win32PInvoke = Files.App.Helpers.NativeFileOperationsHelper;

namespace Files.App.Utils.Serialization
{
	internal class DefaultJsonSettingsDatabase : IJsonSettingsDatabase
	{
		// Fields & Properties

		private IDictionary<string, object?>? _settingsCache;

		private string? _filePath;

		public static readonly JsonSerializerOptions jsonSerializerOptions = new()
		{
			WriteIndented = true
		};

		// Constructor

		public DefaultJsonSettingsDatabase(string jsonFilePath)
		{
			CreateFile(jsonFilePath);
		}

		// Methods

		protected IDictionary<string, object?>? GetFreshSettings()
		{
			string data = ReadFromFile();

			if (string.IsNullOrWhiteSpace(data))
				data = "null";

			try
			{
				return JsonSerializer.Deserialize<ConcurrentDictionary<string, object?>?>(data) ?? new();
			}
			catch (Exception ex)
			{
				// Show a dialog to notify
				if (App.AppModel.ShouldBrokenJsonBeRefreshed)
				{
					return JsonSerializer.Deserialize<ConcurrentDictionary<string, object?>?>("null") ?? new();
				}
				else
				{
					App.AppModel.RaiseReloadJsonSettingsFailedEvent(ex);

					return null;
				}
			}
		}

		protected bool SaveSettings(IDictionary<string, object?> data)
		{
			var jsonData = JsonSerializer.Serialize(data, jsonSerializerOptions);

			return WriteToFile(jsonData);
		}

		public virtual TValue? GetValue<TValue>(string key, TValue? defaultValue = default)
		{
			_settingsCache ??= GetFreshSettings();

			if (_settingsCache is not null && _settingsCache.TryGetValue(key, out var objVal))
			{
				return GetValueFromObject<TValue>(objVal) ?? defaultValue;
			}
			else
			{
				if (_settingsCache is null)
					return defaultValue;

				if (SetValue(key, defaultValue))
					_settingsCache.TryAdd(key, defaultValue);

				return defaultValue;
			}
		}

		public virtual bool SetValue<TValue>(string key, TValue? newValue)
		{
			_settingsCache ??= GetFreshSettings();

			if (_settingsCache is null)
				return false;

			if (_settingsCache.TryAdd(key, newValue))
				return SaveSettings(_settingsCache);
			else
				return UpdateValueInCache(_settingsCache[key]);

			bool UpdateValueInCache(object? value)
			{
				bool isDifferent;

				if (newValue is IEnumerable enumerableNewValue && value is IEnumerable enumerableValue)
				{
					isDifferent = !enumerableValue.Cast<object>().SequenceEqual(enumerableNewValue.Cast<object>());
				}
				else
				{
					isDifferent = value != (object?)newValue;
				}

				if (isDifferent)
				{
					// Values are different, update the value and reload the cache.
					_settingsCache[key] = newValue;

					return SaveSettings(_settingsCache);
				}
				else
				{
					// The cache does not need to be updated, continue.
					return false;
				}
			}
		}

		public virtual bool RemoveKey(string key)
		{
			_settingsCache ??= GetFreshSettings();

			return _settingsCache is not null && _settingsCache.Remove(key) && SaveSettings(_settingsCache);
		}

		public bool FlushSettings()
		{
			// The settings are always flushed automatically, return true.
			return true;
		}

		public virtual bool ImportSettings(object? import)
		{
			try
			{
				// Try convert
				var data = (IDictionary<string, object?>?)import;
				if (data is null)
					return false;

				// Serialize
				var serialized = JsonSerializer.Serialize(data, jsonSerializerOptions);

				// Write to file
				if (!WriteToFile(serialized))
					return false;

				_settingsCache = GetFreshSettings();

				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				Debugger.Break();

				return false;
			}
		}

		public object? ExportSettings()
		{
			return GetFreshSettings();
		}

		protected static TValue? GetValueFromObject<TValue>(object? obj)
		{
			if (obj is JsonElement jElem)
			{
				return jElem.Deserialize<TValue>();
			}

			return (TValue?)obj;
		}

		public bool CreateFile(string path)
		{
			var parentDir = SystemIO.Path.GetDirectoryName(path);
			if (string.IsNullOrEmpty(parentDir))
				return false;

			Win32PInvoke.CreateDirectoryFromApp(parentDir, IntPtr.Zero);

			var hFile = Win32PInvoke.CreateFileFromApp(
				path,
				Win32PInvoke.GENERIC_READ,
				Win32PInvoke.FILE_SHARE_READ,
				IntPtr.Zero,
				Win32PInvoke.OPEN_ALWAYS,
				(uint)Win32PInvoke.File_Attributes.BackupSemantics,
				IntPtr.Zero);

			if (hFile.IsHandleInvalid())
				return false;

			Win32PInvoke.CloseHandle(hFile);

			_filePath = path;
			return true;
		}

		public string ReadFromFile()
		{
			if (string.IsNullOrEmpty(_filePath))
				throw new ArgumentNullException(nameof(_filePath));

			return Win32PInvoke.ReadStringFromFile(_filePath);
		}

		public bool WriteToFile(string? text)
		{
			if (string.IsNullOrEmpty(_filePath))
				throw new ArgumentNullException(nameof(_filePath));

			return Win32PInvoke.WriteStringToFile(_filePath, text);
		}
	}
}
