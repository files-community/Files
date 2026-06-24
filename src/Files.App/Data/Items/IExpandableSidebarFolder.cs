// Copyright (c) Files Community
// Licensed under the MIT License.

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
