using Files.Core.Models;
using Files.Sdk.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Core.Services
{
	/// <summary>
	/// Represents a service used for data to image conversion.
	/// </summary>
	public interface IImageService
    {
        /// <summary>
        /// Gets associated item icon of provided <paramref name="storable"/>. May return null if the icon is inaccessible.
        /// </summary>
        /// <param name="storable">The storable object of which to get the icon.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful, value is <see cref="IImageModel"/> representation of the icon, otherwise null.</returns>
        Task<IImageModel?> GetIconAsync(IStorable storable, CancellationToken cancellationToken);

		[Obsolete("Use GetIconAsync() instead.")]
        Task<IImageModel?> GetImageModelFromDataAsync(byte[]? rawData);

		[Obsolete("Use GetIconAsync() instead.")]
        Task<IImageModel?> GetImageModelFromPathAsync(string filePath, uint thumbnailSize = 64u);
    }
}
