using System.Collections.Generic;

namespace Files.Backend.Services
{
    public interface IStorageEnumeratorService
    {
        /// <summary>
        /// Checks whether the <see cref="IStorageEnumeratorService"/> is available for provided <paramref name="directoryPath"/>.
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        bool IsAvailable(string directoryPath);

        IEnumerable<string> Enumerate(string path); // TODO(i) - instead of returning strings, return a wrapper around WIN32_FIND_DATA (?)
    }
}
