using Files.App.DataModels;
using Files.App.Helpers.LayoutPreferences;
using Files.App.UserControls.MultitaskingControl;
using Files.App.ViewModels.Properties;
using Files.App.Views;
using Files.Shared;
using Files.Shared.Cloud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(string[]))]
    [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(TerminalFileModel))]
    [JsonSerializable(typeof(List<CloudProvider>))]
    [JsonSerializable(typeof(ShellLibraryItem))]
    [JsonSerializable(typeof(List<ShellLibraryItem>))]
    [JsonSerializable(typeof(LayoutPrefsDb.LayoutDbPrefs[]))]
    [JsonSerializable(typeof(IEnumerable<LayoutPrefsDb.LayoutDbPrefs>))]
    [JsonSerializable(typeof(List<ShellLinkItem>))]
    [JsonSerializable(typeof(AppTheme))]
    [JsonSerializable(typeof(List<FileProperty>))]
    [JsonSerializable(typeof(PaneNavigationArguments))]
    [JsonSerializable(typeof(TabItemArguments))]
    [JsonSerializable(typeof(LayoutPreferences.LayoutPreferences))]
    [JsonSerializable(typeof(ShellFileItem))]
    // FIXME: workaround json generator bug: [JsonSerializable(typeof(SidebarPinnedModel))]
    internal partial class JsonContext : JsonSerializerContext
    {
    }
}
