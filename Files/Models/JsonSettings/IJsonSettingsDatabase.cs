namespace Files.Models.JsonSettings
{
    public interface IJsonSettingsDatabase
    {
        object GetValue(string key, object defaultValue = null);

        TValue GetValue<TValue>(string key, object defaultValue = null);

        bool AddKey(string key, object value);

        bool RemoveKey(string key);

        bool UpdateKey(string key, object newValue);

        bool FlushSettings();

        bool ImportSettings(object import);

        object ExportSettings();
    }
}
