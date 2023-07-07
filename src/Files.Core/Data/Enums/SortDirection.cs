// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Enums
{
	/// <summary>
	/// SortDirection is used instead of the CommunityToolkit equivalent because it is tied to the model
	/// </summary>
	public enum SortDirection : byte
	{
		/// <summary>
		/// Sort in ascending order.
		/// </summary>
		Ascending = 0,

		/// <summary>
		/// Sort in descending order.
		/// </summary>
		Descending = 1
	}
}
