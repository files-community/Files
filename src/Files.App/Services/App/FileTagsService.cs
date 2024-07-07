// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;
using Files.Core.Storage.Extensions;
using Files.Core.Storage.Storables;
using System.Runtime.CompilerServices;

namespace Files.App.Services
{
	/// <inheritdoc cref="IFileTagsService"/>
	internal sealed class FileTagsService : IFileTagsService
	{
		private readonly IWindowsRecycleBinService WindowsRecycleBinService = Ioc.Default.GetRequiredService<IWindowsRecycleBinService>();
		private readonly IFileTagsSettingsService FileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		private readonly IStorageService StorageService = Ioc.Default.GetRequiredService<IStorageService>();

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
				if (!item.Tags.Contains(tagUid) || WindowsRecycleBinService.IsRecycled(item.FilePath))
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
	}
}
