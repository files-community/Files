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
        #region Private Members

        private Dictionary<string, object> serializableSettings = new Dictionary<string, object>();

        #endregion

        #region Protected Members

        protected readonly string settingsPath;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes an instance of <see cref="BaseJsonSettingsViewModel"/>, <see cref="Init"/> is called by default
        /// </summary>
        /// <param name="settingsPath"></param>
        public BaseJsonSettingsViewModel(string settingsPath)
        {
            this.settingsPath = settingsPath;
            Init();
        }

        #endregion

        #region Protected Helpers

        private async void Init()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync(PathHelpers.Combine(Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.JsonSettingsFileName), CreationCollisionOption.OpenIfExists);
        }

        #endregion

        #region Get, Set

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

        #endregion
    }
}
