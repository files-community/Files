// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Enums
{
	/// <summary>
	/// Behavior used to retrieve and adjust icons
	/// </summary>
	public enum IconOptions
	{
		/// <summary>
		/// Default. No options.
		/// </summary>
		None,

		/// <summary>
		/// Increase requested size based on the displays DPI setting.
		/// </summary>
		UseCurrentScale,

		/// <summary>
		/// Retrieve only the file icon, even a thumbnail is available. This has the best performance.
		/// </summary>
		ReturnIconOnly,

		/// <summary>
		/// Retrieve a thumbnail only if it is cached or embedded in the file.
		/// </summary>
		ReturnOnlyIfCached,
	}
}
