// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System.Collections.Specialized;

namespace Files.App.Filesystem
{
	public class FileTagsManager
	{
		private readonly ILogger _logger;

		private readonly IFileTagsSettingsService _fileTagsSettingsService;

		public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

		private readonly List<FileTagItem> _FileTags = new();
		public IReadOnlyList<FileTagItem> FileTags
		{
			get
			{
				lock (_FileTags)
					return _FileTags.ToList().AsReadOnly();
			}
		}

		public FileTagsManager()
		{
			// Dependency Injection
			_logger = Ioc.Default.GetRequiredService<ILogger<App>>();
			_fileTagsSettingsService = Ioc.Default.GetService<IFileTagsSettingsService>();

			_fileTagsSettingsService.OnTagsUpdated += TagsUpdated;
		}

		private async void TagsUpdated(object? _, EventArgs e)
		{
			lock (_FileTags)
				_FileTags.Clear();

			DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			await UpdateFileTagsAsync();
		}

		public Task UpdateFileTagsAsync()
		{
			try
			{
				foreach (var tag in _fileTagsSettingsService.FileTagList)
				{
					var tagItem = new FileTagItem()
					{
						Text = tag.Name,
						Path = $"tag:{tag.Name}",
						FileTag = tag,
						MenuOptions = new ContextMenuOptions { IsLocationItem = true },
					};

					lock (_FileTags)
					{
						if (_FileTags.Any(x => x.Path == $"tag:{tag.Name}"))
							continue;

						_FileTags.Add(tagItem);
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
