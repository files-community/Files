using Files.Backend.AppModels;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.FileTags;
using Files.Sdk.Storage.Extensions;
using Files.Sdk.Storage.LocatableStorage;
using System.Runtime.CompilerServices;

namespace Files.App.ServicesImplementation
{
	/// <inheritdoc cref="IFileTagsService"/>
	internal sealed class FileTagsService : IFileTagsService
	{
		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		private readonly IFileTagsSettingsService FileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

		/// <inheritdoc/>
		public Task<bool> IsSupportedAsync()
		{
			return Task.FromResult(true);
		}

		/// <inheritdoc/>
		public Task<bool> SetFileTagAsync(ILocatableStorable storable, string[] tagUids, CancellationToken cancellationToken = default)
		{
			FileTagsHelper.WriteFileTag(storable.Path, tagUids);
			return Task.FromResult(true);
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<TagViewModel> GetTagsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			foreach (var item in FileTagsSettingsService.FileTagList)
				yield return item;

			await Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<TaggedItemModel> GetItemsForTagAsync(string tagUid, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			foreach (var item in FileTagsHelper.GetDbInstance().GetAll())
			{
				if (!item.Tags.Contains(tagUid))
					continue;

				var storable = await StorageService.TryGetStorableFromPathAsync(item.FilePath, cancellationToken);
				if (storable is null)
					continue;

				yield return new(item.Tags, storable);
			}

			await Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<TaggedItemModel> GetAllFileTagsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
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
