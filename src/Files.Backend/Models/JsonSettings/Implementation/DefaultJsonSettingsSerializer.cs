using Newtonsoft.Json;

namespace Files.Backend.Models.JsonSettings.Implementation
{
    public sealed class DefaultJsonSettingsSerializer : IJsonSettingsSerializer
    {
        public T DeserializeFromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public string SerializeToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
