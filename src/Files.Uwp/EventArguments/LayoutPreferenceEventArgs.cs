using static Files.Uwp.ViewModels.FolderSettingsViewModel;

namespace Files.Uwp.EventArguments
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