using static Files.ViewModels.FolderSettingsViewModel;

namespace Files.EventArguments
{
    public class LayoutPreferenceEventArgs
    {
        public readonly LayoutPreferences LayoutPreference;

        public readonly bool IsAdaptiveLayoutUpdateRequired;

        internal LayoutPreferenceEventArgs(LayoutPreferences layoutPref, bool isAdaptiveLayoutUpdateRequired = false)
        {
            LayoutPreference = layoutPref;
            IsAdaptiveLayoutUpdateRequired = isAdaptiveLayoutUpdateRequired;
        }
    }
}