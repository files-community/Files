// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Items
{
	/// <summary>
	/// Sidebar items whose children load lazily on first expansion (DriveItem, LocationItem). Used by the sidebar VM to drive expandability setup without per-type switches.
	/// </summary>
	internal interface IExpandableSidebarFolder
	{
		bool IsExpandableFolder { get; set; }
		bool HasUnrealizedChildren { get; set; }
	}
}
