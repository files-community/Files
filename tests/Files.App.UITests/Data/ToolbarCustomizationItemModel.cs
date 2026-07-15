// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.App.UITests.Data
{
	internal partial class ToolbarCustomizationItemModel : ObservableObject
	{
		public string DisplayName { get; set; } = string.Empty;

		public string IconGlyph { get; set; } = string.Empty;

		public bool HasIcon { get; set; } = true;

		public bool IsSeparator { get; set; }

		[ObservableProperty]
		public partial bool ShowIcon { get; set; } = true;

		[ObservableProperty]
		public partial bool ShowLabel { get; set; }
	}
}
