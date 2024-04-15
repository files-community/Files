namespace Files.Core.Storage.NestedStorage
{
    /// <summary>
    /// Represents a file that resides within a traversable folder structure.
    /// </summary>
    public interface INestedFile : IFile, INestedStorable
    {
    }
}
