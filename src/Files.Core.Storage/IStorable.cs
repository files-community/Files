﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage
{
	/// <summary>
	/// Represents a base storage object on the file system.
	/// </summary>
	public interface IStorable
	{
		/// <summary>
		/// Gets the unique and consistent identifier for this file or folder.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Gets the name of the storage object.
		/// </summary>
		string Name { get; }
	}
}
