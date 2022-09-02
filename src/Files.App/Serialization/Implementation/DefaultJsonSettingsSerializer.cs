using System.Text.Json;

#nullable enable

namespace Files.App.Serialization.Implementation
{
    internal sealed class DefaultJsonSettingsSerializer : IJsonSettingsSerializer
    {
        private readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public string? SerializeToJson(object? obj)
        {
            return JsonSerializer.Serialize(obj, options);
        }

        public T? DeserializeFromJson<T>(string json)
        {
            return JsonSerializer.Deserialize<T?>(json);
        }
    }
}
