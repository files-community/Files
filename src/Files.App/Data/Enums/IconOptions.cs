// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Behavior used to retrieve and adjust icons
	/// </summary>
	[Flags]
	public enum IconOptions
	{
		/// <summary>
		/// Default. No options.
		/// </summary>
		None = 0,

		/// <summary>
		/// Retrieve only the file icon, even a thumbnail is available. This has the best performance.
		/// </summary>
		ReturnIconOnly = 2,

		/// <summary>
		/// Retrieve only the thumbnail.
		/// </summary>
		ReturnThumbnailOnly = 4,

		/// <summary>
		/// Retrieve a thumbnail only if it is cached or embedded in the file.
		/// </summary>
		ReturnOnlyIfCached = 8,

		/// <summary>
		/// Use the exact scaled size instead of snapping up to a standard size. Snapping a small icon up can
		/// make the shell return a folder-style variant instead of the distinct known-folder icon (sidebar).
		/// </summary>
		SkipSizeSnapping = 16,
	}
}
