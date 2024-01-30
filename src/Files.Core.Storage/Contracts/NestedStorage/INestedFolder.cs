// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage.Contracts
{
	/// <summary>
	/// Represents a folder that resides within a traversable folder structure.
	/// </summary>
	public interface INestedFolder : IFolder, INestedStorable
	{
	}
}
