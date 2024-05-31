// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage.NestedStorage
{
    /// <summary>
    /// Represents a folder that resides within a traversable folder structure.
    /// </summary>
    public interface INestedFolder : IFolder, INestedStorable
    {
    }
}
