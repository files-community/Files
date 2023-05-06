// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Backend.Enums
{
	public enum PageLayoutType
	{
		/// <summary>
		/// Don't decide.
		/// Another function to decide can be called afterwards if available.
		/// </summary>
		None,

		/// <summary>
		/// Apply the layout Detail.
		/// </summary>
		Detail,

		/// <summary>
		/// Apply the layout Grid.
		/// </summary>
		Grid,
	}
}
