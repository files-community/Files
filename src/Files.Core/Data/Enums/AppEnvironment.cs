// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Enums
{
	/// <summary>
	/// Defines constants that specify application distribution type.
	/// </summary>
	/// <remarks>
	/// Those type names are mostly corresponded to build configurations.
	/// </remarks>
	public enum AppEnvironment
	{
		/// <summary>
		/// Application distribution type is Dev.
		/// </summary>
		Dev,

		/// <summary>
		/// Application distribution type is Stable.
		/// </summary>
		Stable,

		/// <summary>
		/// Application distribution type is Store.
		/// </summary>
		Store,

		/// <summary>
		/// Application distribution type is Preview.
		/// </summary>
		Preview
	}
}
