﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Enums
{
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
