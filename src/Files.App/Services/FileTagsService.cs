// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;
using Files.Core.Storage.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Services
{
	/// <inheritdoc cref="IFileTagsService"/>
	internal sealed class FileTagsService : IFileTagsService
	{
		private IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		public event EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		private static readonly Lazy<FileTagsDb> database =
			new(() => new FileTagsDb(SystemIO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db"), true));

		/// <inheritdoc/>
		public string FileTagsDatabasePath
			=> SystemIO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db");

		private IList<TagViewModel> _FileTagList = [];
		/// <inheritdoc/>
		public IList<TagViewModel> FileTagList
		{
			get
			{
				lock (_FileTagList)
					return _FileTagList;
			}
		}

		public FileTagsService()
		{
			FileTagsSettingsService.OnTagsUpdated += async (s, e) =>
			{
				lock (_FileTagList)
					_FileTagList.Clear();

				DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

				UpdateFileTagsListAsync();
			};
		}

		/// <inheritdoc/>
		public FileTagsDb GetFileTagsDatabaseInstance()
		{
			return database.Value;
		}

		/// <inheritdoc/>
		public string[] GetFileTagForPath(string filePath)
		{
			var tagString = NativeFileOperationsHelper.ReadStringFromFile($"{filePath}:files");

			if (string.IsNullOrEmpty(tagString))
				return Array.Empty<string>();

			return tagString.Split(',', StringSplitOptions.RemoveEmptyEntries);
		}

		/// <inheritdoc/>
		public async Task<bool> SetFileTagForPathAsync(string path, string[] tags, CancellationToken cancellationToken = default)
		{
			// Backup date modified
			var isDateOk = NativeFileOperationsHelper.GetFileDateModified(path, out var dateModified);

			// Unset read-only attribute (#7534)
			var isReadOnly = NativeFileOperationsHelper.HasFileAttribute(path, SystemIO.FileAttributes.ReadOnly);
			if (isReadOnly)
				NativeFileOperationsHelper.UnsetFileAttribute(path, SystemIO.FileAttributes.ReadOnly);

			if (tags is null || tags.Length == 0)
			{
				NativeFileOperationsHelper.DeleteFileFromApp($"{path}:files");
			}
			else if (GetFileTagForPath(path) is not string[] arr || !tags.SequenceEqual(arr))
			{
				var result = NativeFileOperationsHelper.WriteStringToFile($"{path}:files", string.Join(',', tags));
				if (result == false)
				{
					ContentDialog dialog = new()
					{
						Title = "ErrorApplyingTagTitle".GetLocalizedResource(),
						Content = "ErrorApplyingTagContent".GetLocalizedResource(),
						PrimaryButtonText = "Ok".GetLocalizedResource()
					};

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					await dialog.TryShowAsync();
				}
			}

			// Restore read-only attribute (#7534)
			if (isReadOnly)
				NativeFileOperationsHelper.SetFileAttribute(path, SystemIO.FileAttributes.ReadOnly);

			// Restore data modified
			if (isDateOk)
				NativeFileOperationsHelper.SetFileDateModified(path, dateModified);

			return true;
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<TagViewModel> GetAllTagsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			foreach (var item in FileTagsSettingsService.FileTagList)
				yield return item;

			await Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<TaggedItemModel> GetStorableItemsForFileTagAsync(string tagUid, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			foreach (var item in GetFileTagsDatabaseInstance().GetAll())
			{
				if (!item.Tags.Contains(tagUid) || RecycleBinHelpers.IsPathUnderRecycleBin(item.FilePath))
					continue;

				var storable = await StorageService.TryGetStorableAsync(item.FilePath, cancellationToken);
				if (storable is null)
					continue;

				yield return new(item.Tags, storable);
			}

			await Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<TaggedItemModel> GetStorableItemsForAllFileTagsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			foreach (var item in GetFileTagsDatabaseInstance().GetAll())
			{
				var storable = await StorageService.TryGetStorableAsync(item.FilePath, cancellationToken);
				if (storable is null)
					continue;

				yield return new(item.Tags, storable);
			}

			await Task.CompletedTask;
		}

		public async Task UpdateFileTagsListAsync()
		{
			await foreach (var item in GetAllTagsAsync())
			{
				_FileTagList.Add(item);

				DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
			}
		}

		/// <inheritdoc/>
		public void UpdateFileTagsDatabase()
		{
			var database = GetFileTagsDatabaseInstance();

			foreach (var file in database.GetAll())
			{
				var pathFromFrn = Win32API.PathFromFileId(file.Frn ?? 0, file.FilePath);
				if (pathFromFrn is not null)
				{
					// Frn is valid, update file path
					var tag = GetFileTagForPath(pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal));
					if (tag is not null && tag.Any())
					{
						database.UpdateTag(file.Frn ?? 0, null, pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal));
						database.SetTags(pathFromFrn.Replace(@"\\?\", "", StringComparison.Ordinal), file.Frn, tag);
					}
					else
					{
						database.SetTags(null, file.Frn, null);
					}
				}
				else
				{
					var tag = GetFileTagForPath(file.FilePath);
					if (tag is not null && tag.Any())
					{
						if (!SafetyExtensions.IgnoreExceptions(() =>
						{
							var frn = NativeFileOperationsHelper.GetFileFRN(file.FilePath);
							database.UpdateTag(file.FilePath, frn, null);
							database.SetTags(file.FilePath, frn, tag);
						}, App.Logger))
						{
							database.SetTags(file.FilePath, null, null);
						}
					}
					else
					{
						database.SetTags(file.FilePath, null, null);
					}
				}
			}
		}
	}
}
