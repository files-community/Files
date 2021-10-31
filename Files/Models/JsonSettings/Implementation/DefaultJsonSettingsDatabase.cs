using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Files.Common;

namespace Files.Models.JsonSettings.Implementation
{
    public class DefaultJsonSettingsDatabase : IJsonSettingsDatabase
    {
        protected Dictionary<string, object> settingsCache;

        protected readonly IJsonSettingsSerializer jsonSettingsSerializer;

        protected readonly ISettingsSerializer settingsSerializer;

        public DefaultJsonSettingsDatabase(IJsonSettingsSerializer jsonSettingsSerializer, ISettingsSerializer settingsSerializer)
        {
            this.jsonSettingsSerializer = jsonSettingsSerializer;
            this.settingsSerializer = settingsSerializer;
            this.settingsCache = new Dictionary<string, object>();
        }

        protected virtual Dictionary<string, object> GetNewSettingsCache()
        {
            string settingsData = settingsSerializer.ReadFromFile();

            return jsonSettingsSerializer.DeserializeFromJson<Dictionary<string, object>>(settingsData) ?? new Dictionary<string, object>();
        }

        protected virtual bool SaveSettingsCache()
        {
            string settingsData = jsonSettingsSerializer.SerializeToJson(this.settingsCache);

            return settingsSerializer.WriteToFile(settingsData);
        }

        public virtual TValue GetValue<TValue>(string key, TValue defaultValue = default)
        {
            var value = GetObjectValue(key, defaultValue);
            if (value is Newtonsoft.Json.Linq.JToken jTokenValue)
            {
                return jTokenValue.ToObject<TValue>();
            }
            return (TValue)value;
        }

        private object GetObjectValue(string key, object defaultValue = null)
        {
            this.settingsCache = GetNewSettingsCache();

            if (settingsCache.ContainsKey(key))
            {
                return settingsCache[key];
            }
            else
            {
                AddKey(key, defaultValue);
                return defaultValue;
            }
        }

        public virtual bool AddKey(string key, object value)
        {
            this.settingsCache = GetNewSettingsCache();

            if (!this.settingsCache.ContainsKey(key))
            {
                this.settingsCache.Add(key, value);

                return SaveSettingsCache();
            }

            return false;
        }

        public virtual bool RemoveKey(string key)
        {
            this.settingsCache = GetNewSettingsCache();

            if (this.settingsCache.ContainsKey(key))
            {
                this.settingsCache.Remove(key);

                return SaveSettingsCache();
            }

            return false;
        }

        public virtual bool UpdateKey(string key, object newValue)
        {
            this.settingsCache = GetNewSettingsCache();

            if (!this.settingsCache.ContainsKey(key))
            {
                return AddKey(key, newValue);
            }
            else
            {
                this.settingsCache[key] = newValue;

                return SaveSettingsCache();
            }
        }

        public virtual bool ImportSettings(object import)
        {
            try
            {
                // Try convert
                settingsCache = (Dictionary<string, object>)import;

                // Serialize
                string serialized = jsonSettingsSerializer.SerializeToJson(this.settingsCache);

                // Write to file
                settingsSerializer.WriteToFile(serialized);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Debugger.Break();

                return false;
            }
        }

        public virtual object ExportSettings()
        {
            settingsCache = GetNewSettingsCache();

            return settingsCache;
        }

        public virtual bool FlushSettings()
        {
            try
            {
                // Serialize
                string serialized = jsonSettingsSerializer.SerializeToJson(this.settingsCache);

                // Write to file
                settingsSerializer.WriteToFile(serialized);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Debugger.Break();

                return false;
            }
        }

        public Dictionary<string, object> TakeDifferent(Dictionary<string, object> other)
        {
            Dictionary<string, object> difference = new Dictionary<string, object>();

            foreach (var item in other)
            {
                foreach (var item2 in settingsCache)
                {
                    if (item.Key == item2.Key && (!item.Value?.Equals(item2.Value) ?? false))
                    {
                        difference.Add(item.Key, item.Value);
                    }
                }
            }

            return difference;
        }
    }
}
