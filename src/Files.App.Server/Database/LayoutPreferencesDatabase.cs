// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Server.Data;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Windows.ApplicationModel;
using static Files.App.Server.Utils.RegistryUtils;

namespace Files.App.Server.Database
{
	public sealed class LayoutPreferencesDatabase
	{
		private readonly static string LayoutSettingsKey = @$"Software\Files Community\Files\{Package.Current.Id.FullName}\LayoutPreferences";

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
			ResetAll();
			var preferences = JsonSerializer.Deserialize<LayoutPreferences[]>(json);
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
	}
}
