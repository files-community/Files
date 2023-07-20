// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public class LayoutPreferenceEventArgs
	{
		public readonly bool IsAdaptiveLayoutUpdateRequired;

		public readonly LayoutPreferenceManager LayoutPreference;

		internal LayoutPreferenceEventArgs(LayoutPreferenceManager layoutPref)
			=> LayoutPreference = layoutPref;

		internal LayoutPreferenceEventArgs(LayoutPreferenceManager layoutPref, bool isAdaptiveLayoutUpdateRequired)
			=> (LayoutPreference, IsAdaptiveLayoutUpdateRequired) = (layoutPref, isAdaptiveLayoutUpdateRequired);
	}
}
