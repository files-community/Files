// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.Core.Storage.Storables
{
	/// <summary>
	/// Represents a file or folder that resides within a folder structure.
	/// </summary>
	public interface ILocatableStorable : IStorable
	{
		/// <summary>
		/// Gets the path where the item resides.
		/// </summary>
		string Path { get; }
	}
}
