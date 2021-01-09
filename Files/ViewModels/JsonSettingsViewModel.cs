using Files.Helpers;
using Files.SettingsInterfaces;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace Files.ViewModels
{
	public class JsonSettingsViewModel : ObservableObject, IJsonSettings
	{
		#region Private Members

		private readonly string settingsPath;

		private Dictionary<string, object> serializableSettings = new Dictionary<string, object>();

		#endregion

		#region Constructor

		public JsonSettingsViewModel()
		{
			settingsPath = PathHelpers.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.JsonSettingsFileName);
			serializableSettings = new Dictionary<string, object>();
			Init();
		}

		#endregion

		#region Private Helpers

		private async void Init()
		{
			await ApplicationData.Current.LocalFolder.CreateFileAsync(PathHelpers.Combine(Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.JsonSettingsFileName), CreationCollisionOption.OpenIfExists);
		}

		#endregion

		#region IJsonSettings

		public Dictionary<string, List<string>> SavedBundles
		{
			get => Get<Dictionary<string, List<string>>>(null);
			set => Set(value);
		}

		#endregion

		#region Get, Set

		private bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = "")
		{
			try
			{
				if (!serializableSettings.ContainsKey(propertyName))
				{
					serializableSettings.Add(propertyName, value);
				}
				else
				{
					serializableSettings[propertyName] = value;
				}

				// Serialize
				NativeFileOperationsHelper.WriteStringToFile(settingsPath, JsonConvert.SerializeObject(serializableSettings, Formatting.Indented));
			}
			catch (Exception e)
			{
				Debugger.Break();
				return false;
			}
			return true;
		}

		private TValue Get<TValue>(TValue defaultValue, [CallerMemberName] string propertyName = "")
		{
			try
			{
				string settingsData = NativeFileOperationsHelper.ReadStringFromFile(settingsPath);

				Dictionary<string, TValue> rawData = JsonConvert.DeserializeObject<Dictionary<string, TValue>>(settingsData);
				Dictionary<string, object> convertedData = new Dictionary<string, object>();

				if (rawData != null)
				{
					foreach (var item in rawData)
					{
						convertedData.Add(item.Key, (TValue)item.Value);
					}
				}

				serializableSettings = convertedData;

				if (serializableSettings == null)
				{
					serializableSettings = new Dictionary<string, object>();
				}

				if (!serializableSettings.ContainsKey(propertyName))
				{
					serializableSettings.Add(propertyName, defaultValue);

					// Serialize
					NativeFileOperationsHelper.WriteStringToFile(settingsPath, JsonConvert.SerializeObject(serializableSettings, Formatting.Indented));
				}

				return (TValue)serializableSettings[propertyName];
			}
			catch (Exception e)
			{
				Debugger.Break();
				return default(TValue);
			}
		}

		#endregion
	}
}
