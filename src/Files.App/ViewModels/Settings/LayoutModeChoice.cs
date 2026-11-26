// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents a layout that a layout template can use.
	/// </summary>
	public sealed partial class LayoutModeChoice(FolderLayoutModes layoutMode, string displayName, string iconStyleKey) : ObservableObject
	{
		public FolderLayoutModes LayoutMode { get; } = layoutMode;

		public string DisplayName { get; } = displayName;

		/// <summary>
		/// Gets the key of the themed icon style of the layout.
		/// </summary>
		public string IconStyleKey { get; } = iconStyleKey;

		[ObservableProperty]
		public partial bool IsSelected { get; set; }
	}
}
