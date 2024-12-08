// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage.Storables
{
    /// <summary>
    /// Represents a folder that resides within a traversable folder structure.
    /// </summary>
    public interface INestedFolder : IFolder, INestedStorable
    {
    }
}
