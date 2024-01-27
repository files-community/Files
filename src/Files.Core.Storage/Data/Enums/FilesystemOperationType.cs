// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage
{
	public enum FilesystemOperationType
	{
		/// <summary>
		/// Copy storage object.
		/// </summary>
		Copy = 0,

		/// <summary>
		/// Move storage object.
		/// </summary>
		Move = 1,

		/// <summary>
		/// Delete storage object.
		/// </summary>
		Delete = 2
	}
}
