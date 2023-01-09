using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Filesystem;
using Files.Backend.AppModels;
using Files.Backend.Services;
using Files.Sdk.Storage;
using Files.Sdk.Storage.Extensions;
using Files.Sdk.Storage.LocatableStorage;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.ServicesImplementation
{
	/// <inheritdoc cref="IFileTagsService"/>
	internal sealed class FileTagsService : IFileTagsService
	{
		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		/// <inheritdoc/>
		public Task<bool> IsSupportedAsync()
		{
			return Task.FromResult(true);
		}

		/// <inheritdoc/>
		public Task<bool> SetFileTagAsync(ILocatableStorable storable, string[] tags, CancellationToken cancellationToken)
		{
			FileTagsHelper.WriteFileTag(storable.Path, tags);
			return Task.FromResult(true);
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<string> GetTagsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
		{
			foreach (var item in FileTagsHelper.GetDbInstance().GetAll())
			{
				yield return "Blue"; // TODO: Get tag types
			}

			await Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<TagModel> GetItemsForTagAsync(string tag, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			foreach (var item in FileTagsHelper.GetDbInstance().GetAll())
			{
				if (!item.Tags.Contains(tag))
					continue;

				var storable = await StorageService.TryGetStorableFromPathAsync(item.FilePath, cancellationToken);
				if (storable is null)
					continue;

				yield return new(item.Tags, storable);
			}

			await Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<TagModel> GetAllFileTagsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
		{
			foreach (var item in FileTagsHelper.GetDbInstance().GetAll())
			{
				var storable = await StorageService.TryGetStorableFromPathAsync(item.FilePath, cancellationToken);
				if (storable is null)
					continue;

				yield return new(item.Tags, storable);
			}

			await Task.CompletedTask;
		}
	}
}
