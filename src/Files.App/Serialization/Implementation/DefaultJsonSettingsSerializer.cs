using Files.App.Helpers;
using System.Text.Json;

#nullable enable

namespace Files.App.Serialization.Implementation
{
    internal sealed class DefaultJsonSettingsSerializer : IJsonSettingsSerializer
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static readonly JsonContext Context = new(Options);

        public string? SerializeToJson(object? obj)
        {
            return JsonSerializer.Serialize(obj, Options);
        }

        public T? DeserializeFromJson<T>(string json)
        {
            return JsonSerializer.Deserialize<T?>(json);
        }
    }
}
