// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public sealed class LayoutPreferenceEventArgs
	{
		public readonly bool IsAdaptiveLayoutUpdateRequired;

		public readonly LayoutPreferencesItem LayoutPreference;

		internal LayoutPreferenceEventArgs(LayoutPreferencesItem layoutPref)
			=> LayoutPreference = layoutPref;

		internal LayoutPreferenceEventArgs(LayoutPreferencesItem layoutPref, bool isAdaptiveLayoutUpdateRequired)
			=> (LayoutPreference, IsAdaptiveLayoutUpdateRequired) = (layoutPref, isAdaptiveLayoutUpdateRequired);
	}
}
