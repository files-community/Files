// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Represents enabled network sharing capabilities.
	/// </summary>
	[Flags]
	public enum NetworkAvailability
	{
		/// <summary>
		/// Neither network discovery nor file sharing is enabled.
		/// </summary>
		None = 0x0,

		/// <summary>
		/// Network discovery is enabled.
		/// </summary>
		Discovery = 0x1,

		/// <summary>
		/// File sharing is enabled.
		/// </summary>
		Sharing = 0x2,

		/// <summary>
		/// Network discovery and file sharing are enabled.
		/// </summary>
		All = Discovery | Sharing
	}
}
