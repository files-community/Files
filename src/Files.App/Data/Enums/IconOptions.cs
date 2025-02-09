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
		/// Increase requested size based on the displays DPI setting.
		/// </summary>
		UseCurrentScale = 1,

		/// <summary>
		/// Scale the thumbnail to the requested size.
		/// </summary>
		ResizeThumbnail = 2,

		/// <summary>
		/// Retrieve only the file icon, even a thumbnail is available. This has the best performance.
		/// </summary>
		ReturnIconOnly = 4,

		/// <summary>
		/// Retrieve only the thumbnail.
		/// </summary>
		ReturnThumbnailOnly = 8,

		/// <summary>
		/// Retrieve a thumbnail only if it is cached or embedded in the file.
		/// </summary>
		ReturnOnlyIfCached = 16,

		/// <summary>
		/// Default. Retrieve a thumbnail to display a preview of any single item (like a file, folder, or file group).
		/// </summary>
		SingleItem = 32,

		/// <summary>
		/// Retrieve a thumbnail to display previews of files (or other items) in a list.
		/// </summary>
		ListView = 64,
	}
}
