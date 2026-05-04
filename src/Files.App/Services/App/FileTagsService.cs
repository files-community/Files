// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

namespace Files.App.Services
{
	/// <inheritdoc cref="IFileTagsService"/>
	internal sealed class FileTagsService : IFileTagsService
	{
		private readonly IStorageTrashBinService StorageTrashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();
		private readonly IFileTagsSettingsService FileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		private readonly IStorageService StorageService = Ioc.Default.GetRequiredService<IStorageService>();

		/// <inheritdoc/>
		public Task<bool> IsSupportedAsync()
		{
			return Task.FromResult(true);
		}

		/// <inheritdoc/>
		public Task<bool> SetFileTagAsync(IStorable storable, string[] tagUids, CancellationToken cancellationToken = default)
		{
			FileTagsHelper.WriteFileTag(storable.Id, tagUids);
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
				if (!item.Tags.Contains(tagUid) || StorageTrashBinService.IsUnderTrashBin(item.FilePath))
					continue;

				var storable = await StorageService.TryGetStorableAsync(item.FilePath, cancellationToken);
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
				var storable = await StorageService.TryGetStorableAsync(item.FilePath, cancellationToken);
				if (storable is null)
					continue;

				yield return new(item.Tags, storable);
			}

			await Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async Task<ILookup<string, TaggedItemModel>> GetAllItemsGroupedByTagAsync(CancellationToken cancellationToken = default)
		{
			var allDbItems = FileTagsHelper.GetDbInstance().GetAll().ToList();
			var result = new List<(string TagUid, TaggedItemModel Model)>();

			foreach (var dbItem in allDbItems)
			{
				if (StorageTrashBinService.IsUnderTrashBin(dbItem.FilePath))
					continue;

				var storable = await StorageService.TryGetStorableAsync(dbItem.FilePath, cancellationToken);
				if (storable is null)
					continue;

				var model = new TaggedItemModel(dbItem.Tags, storable);
				foreach (var tagUid in dbItem.Tags)
					result.Add((tagUid, model));
			}

			return result.ToLookup(x => x.TagUid, x => x.Model);
		}
	}
}
