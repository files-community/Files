using Files.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace Files.ViewModels
{
    public abstract class BaseJsonSettingsViewModel
    {
        protected readonly bool initialized = false;
        protected readonly string settingsPath;
        protected Dictionary<string, object> serializableSettings = new Dictionary<string, object>();

        /// <summary>
        /// Initializes an instance of <see cref="BaseJsonSettingsViewModel"/>, <see cref="Init"/> is called by default
        /// </summary>
        /// <param name="settingsPath"></param>
        public BaseJsonSettingsViewModel(string settingsPath)
        {
            this.settingsPath = settingsPath;

            Init();
            initialized = true;
        }

        public virtual object ExportSettings()
        {
            return serializableSettings;
        }

        public virtual void ImportSettings(object import)
        {
            try
            {
                serializableSettings = (Dictionary<string, object>)import;
            }
            catch { }
        }

        public virtual bool NotifyOnValueUpdated<TValue>(TValue value, string propertyName)
        {
            return Set(value, propertyName);
        }

        protected virtual TValue Get<TValue>(TValue defaultValue, [CallerMemberName] string propertyName = "")
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

        protected virtual async void Init()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync(System.IO.Path.Combine(Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.BundlesSettingsFileName), CreationCollisionOption.OpenIfExists);
        }

        protected virtual bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = "")
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
    }
}