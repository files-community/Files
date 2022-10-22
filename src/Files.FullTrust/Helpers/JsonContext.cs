using Files.Shared;
using Files.Shared.Cloud;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Files.FullTrust.Helpers
{
    [JsonSerializable(typeof(ShellLibraryItem))]
    [JsonSerializable(typeof(ShellFileItem))]
    [JsonSerializable(typeof(List<ShellLibraryItem>))]
    [JsonSerializable(typeof(string[]))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(IEnumerable<ICloudProvider>))]
    [JsonSerializable(typeof(List<ShellLinkItem>))]
    [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    internal partial class JsonContext : JsonSerializerContext
    {

    }
}
