using System.Collections.Generic;

namespace Files.Models.JsonSettings
{
    public interface IJsonSettingsDatabase
    {
        TValue GetValue<TValue>(string key, TValue defaultValue = default);

        bool AddKey(string key, object value);

        bool RemoveKey(string key);

        bool UpdateKey(string key, object newValue);

        bool FlushSettings();

        bool ImportSettings(object import);

        object ExportSettings();

        Dictionary<string, object> TakeDifferent(Dictionary<string, object> other);
    }
}
