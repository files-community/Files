using Files.ViewModels;
using static Files.ViewModels.FolderSettingsViewModel;

namespace Files.EventArguments
{
    public class LayoutPreferenceEventArgs
    {
        public readonly LayoutPreferences LayoutPreference;

        public readonly bool IsAdaptiveLayoutUpdateRequired;

        public readonly FolderSettingsViewModel FolderSettingsViewModel;

        internal LayoutPreferenceEventArgs(LayoutPreferences layoutPref, FolderSettingsViewModel folderSettingsViewModel, bool isAdaptiveLayoutUpdateRequired = false)
        {
            this.LayoutPreference = layoutPref;
            this.FolderSettingsViewModel = folderSettingsViewModel;
            this.IsAdaptiveLayoutUpdateRequired = isAdaptiveLayoutUpdateRequired;
        }
    }
}
