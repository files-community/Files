using static Files.ViewModels.FolderSettingsViewModel;

namespace Files.EventArguments
{
    public class LayoutPreferenceEventArgs
    {
        public readonly bool IsAdaptiveLayoutUpdateRequired;
        public readonly LayoutPreferences LayoutPreference;

        internal LayoutPreferenceEventArgs(LayoutPreferences layoutPref, bool isAdaptiveLayoutUpdateRequired = false)
        {
            LayoutPreference = layoutPref;
            IsAdaptiveLayoutUpdateRequired = isAdaptiveLayoutUpdateRequired;
        }
    }
}