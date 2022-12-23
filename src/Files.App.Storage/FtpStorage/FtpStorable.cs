using Files.Sdk.Storage.LocatableStorage;
using FluentFTP;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.FtpStorage
{
	/// <inheritdoc cref="IStorable"/>
	public abstract class FtpStorable : ILocatableStorable
	{
		/// <inheritdoc/>
		public string Id => string.Empty;

		/// <inheritdoc/>
		public string Name { get; }

		/// <inheritdoc/>
		public string Path { get; }

		protected internal FtpStorable(string path, string name)
		{
			Name = name;
			Path = path.GetFtpPath();
		}

		public virtual Task<ILocatableFolder?> GetParentAsync(CancellationToken cancellationToken = default)
			=> Task.FromResult<ILocatableFolder?>(null);

		protected async Task<AsyncFtpClient> GetFtpClient(CancellationToken cancellationToken = default)
		{
			AsyncFtpClient client = Path.GetFtpClient();
			await client.EnsureConnectedAsync(cancellationToken);
			return client;
		}
	}
}
