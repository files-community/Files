﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Defines constants that specify git operation result.
	/// </summary>
	public enum GitOperationResult
	{
		/// <summary>
		/// Operation completed successfully.
		/// </summary>
		Success,

		/// <summary>
		/// Operation failed due to an authorization error.
		/// </summary>
		AuthorizationError,

		/// <summary>
		/// Operation failed.
		/// </summary>
		GenericError
	}
}
