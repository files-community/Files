using System.Text.Json;

#nullable enable

namespace Files.App.Serialization.Implementation
{
    internal sealed class DefaultJsonSettingsSerializer : IJsonSettingsSerializer
    {
        public string? SerializeToJson(object? obj)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            return JsonSerializer.Serialize(obj, options);
        }

        public T? DeserializeFromJson<T>(string json)
        {
            return JsonSerializer.Deserialize<T?>(json);
        }
    }
}
