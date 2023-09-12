// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Enums
{
	/// <summary>
	/// Defines constants that specify policy types of confirmation of storage item deletion.
	/// </summary>
	public enum DeleteConfirmationPolicies : byte
	{
		/// <summary>
		/// Always ask to confirm.
		/// </summary>
		Always,

		/// <summary>
		/// Permanent deletion only.
		/// </summary>
		PermanentOnly,

		/// <summary>
		/// Never ask to confirm.
		/// </summary>
		Never,
	}
}
