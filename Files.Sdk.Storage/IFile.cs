using System.IO;
using System.Threading.Tasks;

namespace Files.Sdk.Storage
{
    public interface IFile : IBaseStorage
    {
        string Extension { get; }

        Task<Stream?> OpenStreamAsync(FileAccess access);

        Task<Stream?> OpenStreamAsync(FileAccess access, FileShare share);

        Task<Stream> GetThumbnailStreamAsync(uint requestedSize); // TODO: Return IImage nullable
    }
}
