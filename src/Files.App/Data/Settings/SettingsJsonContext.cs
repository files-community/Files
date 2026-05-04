// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Settings;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Settings))]
[JsonSerializable(typeof(ActionWithParameterItem))]
[JsonSerializable(typeof(List<ActionWithParameterItem>))]
[JsonSerializable(typeof(TagViewModel))]
[JsonSerializable(typeof(List<TagViewModel>))]
[JsonSerializable(typeof(ToolbarItemSettingsEntry))]
[JsonSerializable(typeof(List<ToolbarItemSettingsEntry>))]
[JsonSerializable(typeof(Dictionary<string, bool>))]
[JsonSerializable(typeof(Dictionary<string, List<string>>))]
[JsonSerializable(typeof(Dictionary<string, List<ToolbarItemSettingsEntry>>))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext
{
}
