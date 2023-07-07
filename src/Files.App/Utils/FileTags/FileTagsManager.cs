// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System.Collections.Specialized;

namespace Files.App.Utils.FileTags
{
	public class FileTagsManager
	{
		private readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger<App>>();

		private readonly IFileTagsSettingsService _fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

		public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

		private readonly List<FileTagItem> fileTags = new();
		public IReadOnlyList<FileTagItem> FileTags
		{
			get
			{
				lock (fileTags)
					return fileTags.ToList().AsReadOnly();
			}
		}

		public FileTagsManager()
		{
			_fileTagsSettingsService.OnTagsUpdated += TagsUpdated;
		}

		private async void TagsUpdated(object? _, EventArgs e)
		{
			lock (fileTags)
				fileTags.Clear();

			DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			await UpdateFileTagsAsync();
		}

		public Task UpdateFileTagsAsync()
		{
			try
			{
				foreach (var tag in _fileTagsSettingsService.FileTagList)
				{
					var tagItem = new FileTagItem
					{
						Text = tag.Name,
						Path = $"tag:{tag.Name}",
						FileTag = tag,
						MenuOptions = new ContextMenuOptions { IsLocationItem = true },
					};

					lock (fileTags)
					{
						if (fileTags.Any(x => x.Path == $"tag:{tag.Name}"))
							continue;

						fileTags.Add(tagItem);
					}

					DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, tagItem));
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error loading tags section.");
			}

			return Task.CompletedTask;
		}
	}
}
