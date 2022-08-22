using Files.Uwp.Helpers.LayoutPreferences;

namespace Files.Uwp.EventArguments
{
    public class LayoutPreferenceEventArgs
    {
        public readonly bool IsAdaptiveLayoutUpdateRequired;

        public readonly LayoutPreferences LayoutPreference;

        internal LayoutPreferenceEventArgs(LayoutPreferences layoutPref)
            => LayoutPreference = layoutPref;

        internal LayoutPreferenceEventArgs(LayoutPreferences layoutPref, bool isAdaptiveLayoutUpdateRequired)
            => (LayoutPreference, IsAdaptiveLayoutUpdateRequired) = (layoutPref, isAdaptiveLayoutUpdateRequired);
    }
}