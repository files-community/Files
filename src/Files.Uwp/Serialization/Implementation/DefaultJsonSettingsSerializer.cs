using Newtonsoft.Json;

#nullable enable

namespace Files.Uwp.Serialization.Implementation
{
    internal sealed class DefaultJsonSettingsSerializer : IJsonSettingsSerializer
    {
        public string? SerializeToJson(object? obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public T? DeserializeFromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T?>(json);
        }
    }
}
