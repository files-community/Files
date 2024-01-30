// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage.Contracts
{
	/// <summary>
	/// Represents a file that resides within a traversable folder structure.
	/// </summary>
	public interface INestedFile : IFile, INestedStorable
	{
	}
}
