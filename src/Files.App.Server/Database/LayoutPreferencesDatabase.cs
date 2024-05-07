// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Server.Data;
using LiteDB;
using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Storage;
using static Files.App.Server.Data.LayoutPreferencesRegistry;
using static Files.App.Server.Utils.RegistryUtils;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Files.App.Server.Database
{
	public sealed class LayoutPreferencesDatabase : IDisposable
	{
		private readonly static string LayoutSettingsKey = @$"Software\Files Community\{Package.Current.Id.FullName}\v1\LayoutPreferences";

		private readonly static string LayoutSettingsDbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "user_settings.db");
		private const string LayoutSettingsCollectionName = "layoutprefs";

		private readonly GCHandle _handle;
		private bool _disposed = false;

		static LayoutPreferencesDatabase()
		{
			if (File.Exists(LayoutSettingsDbPath))
			{
				using (var database = new LiteDatabase(new ConnectionString(LayoutSettingsDbPath)
				{
					Connection = ConnectionType.Direct,
					Upgrade = true
				}))
				{
					ImportCore(database.GetCollection<LayoutPreferences>(LayoutSettingsCollectionName).FindAll().ToArray());
				}

				File.Delete(LayoutSettingsDbPath);
			}
		}

		public LayoutPreferencesDatabase()
		{
			throw new NotSupportedException($"Instantiating {nameof(LayoutPreferencesDatabase)} by non-parameterized constructor is not supported.");
		}

		public LayoutPreferencesDatabase(int processId)
		{
			_handle = GCHandle.Alloc(this, GCHandleType.Pinned);

			if (AppInstanceMonitor.AppInstanceResources.TryGetValue(processId, out var instances))
			{
				instances.Add(this);
			}
			else
			{
				AppInstanceMonitor.AppInstanceResources[processId] = [this];
			}
		}

		public LayoutPreferencesItem? GetPreferences(string filePath, ulong? frn)
		{
			return FindPreferences(filePath, frn)?.LayoutPreferencesManager;
		}

		public void SetPreferences(string filePath, ulong? frn, LayoutPreferencesItem? preferencesItem)
		{
			var tmp = FindPreferences(filePath, frn);

			if (tmp is null)
			{
				if (preferencesItem is not null)
				{
					// Insert new tagged file (Id will be auto-incremented)
					var newPref = new LayoutPreferences()
					{
						FilePath = filePath,
						Frn = frn,
						LayoutPreferencesManager = preferencesItem
					};

					UpdateValues(newPref);
				}
			}
			else
			{
				if (preferencesItem is not null)
				{
					// Update file tag
					tmp.LayoutPreferencesManager = preferencesItem;

					UpdateValues(tmp);
				}
				else
				{
					// Remove file tag
					UpdateValues(null);
				}
			}

			void UpdateValues(LayoutPreferences? preferences)
			{
				if (filePath is not null)
				{
					using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(LayoutSettingsKey, filePath));
					SaveValues(filePathKey, preferences);
				}

				if (frn is not null)
				{
					using var frnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(LayoutSettingsKey, "FRN", frn.Value.ToString()));
					SaveValues(frnKey, preferences);
				}
			}
		}

		public void ResetAll()
		{
			Registry.CurrentUser.DeleteSubKeyTree(LayoutSettingsKey, false);
		}

		public void Import(string json)
		{
			var preferences = JsonSerializer.Deserialize<LayoutPreferences[]>(json);
			ImportCore(preferences);
		}


		private static void ImportCore(LayoutPreferences[]? preferences)
		{
			Registry.CurrentUser.DeleteSubKeyTree(LayoutSettingsKey, false);
			if (preferences is null)
			{
				return;
			}
			foreach (var preference in preferences)
			{
				using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(LayoutSettingsKey, preference.FilePath));
				SaveValues(filePathKey, preference);
				if (preference.Frn is not null)
				{
					using var frnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(LayoutSettingsKey, "FRN", preference.Frn.Value.ToString()));
					SaveValues(frnKey, preference);
				}
			}
		}

		public string Export()
		{
			var list = new List<LayoutPreferences>();
			IterateKeys(list, LayoutSettingsKey, 0);
			return JsonSerializer.Serialize(list);
		}

		private void IterateKeys(List<LayoutPreferences> list, string path, int depth)
		{
			using var key = Registry.CurrentUser.OpenSubKey(path);
			if (key is null)
			{
				return;
			}

			if (key.ValueCount > 0)
			{
				var preference = new LayoutPreferences();
				BindValues(key, preference);
				list.Add(preference);
			}

			foreach (var subKey in key.GetSubKeyNames())
			{
				if (depth == 0 && subKey == "FRN")
				{
					// Skip FRN key
					continue;
				}

				IterateKeys(list, CombineKeys(path, subKey), depth + 1);
			}
		}

		private LayoutPreferences? FindPreferences(string filePath, ulong? frn)
		{
			if (filePath is not null)
			{
				using var filePathKey = Registry.CurrentUser.CreateSubKey(CombineKeys(LayoutSettingsKey, filePath));
				if (filePathKey.ValueCount > 0)
				{
					var preference = new LayoutPreferences();
					BindValues(filePathKey, preference);
					if (frn is not null)
					{
						// Keep entry updated
						preference.Frn = frn;
						var value = frn.Value;
						filePathKey.SetValue(nameof(LayoutPreferences.Frn), Unsafe.As<ulong, long>(ref value), RegistryValueKind.QWord);
					}
					return preference;
				}
			}

			if (frn is not null)
			{
				using var frnKey = Registry.CurrentUser.CreateSubKey(CombineKeys(LayoutSettingsKey, "FRN", frn.Value.ToString()));
				if (frnKey.ValueCount > 0)
				{
					var preference = new LayoutPreferences();
					BindValues(frnKey, preference);
					if (filePath is not null)
					{
						// Keep entry updated
						preference.FilePath = filePath;
						frnKey.SetValue(nameof(LayoutPreferences.FilePath), filePath, RegistryValueKind.String);
					}
					return preference;
				}
			}

			return null;
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_handle.Free();
			}
		}
	}
}
