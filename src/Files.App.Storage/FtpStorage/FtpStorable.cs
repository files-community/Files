using Files.Sdk.Storage.LocatableStorage;
using Files.Core.Helpers;
using FluentFTP;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.FtpStorage
{
	public abstract class FtpStorable : ILocatableStorable
	{
		private string? _computedId;

		/// <inheritdoc/>
		public string Path { get; protected set; }

		/// <inheritdoc/>
		public string Name { get; protected set; }

		/// <inheritdoc/>
		public virtual string Id => _computedId ??= ChecksumHelpers.CalculateChecksumForPath(Path);

		protected internal FtpStorable(string path, string name)
		{
			Path = FtpHelpers.GetFtpPath(path);
			Name = name;
		}

		/// <inheritdoc/>
		public virtual Task<ILocatableFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult<ILocatableFolder?>(null);
		}

		protected AsyncFtpClient GetFtpClient()
		{
			return FtpHelpers.GetFtpClient(Path);
		}
	}
}
