// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Data.Items;
using Files.Backend.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Filesystem
{
	public class FileTagsManager
	{
		private readonly ILogger logger = Ioc.Default.GetRequiredService<ILogger<App>>();
		private readonly IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetService<IFileTagsSettingsService>();

		public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

		private readonly List<FileTagItem> fileTags = new();
		public IReadOnlyList<FileTagItem> FileTags
		{
			get
			{
				lock (fileTags)
				{
					return fileTags.ToList().AsReadOnly();
				}
			}
		}

		public FileTagsManager()
		{
			fileTagsSettingsService.OnTagsUpdated += TagsUpdated;
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
				foreach (var tag in fileTagsSettingsService.FileTagList)
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
						{
							continue;
						}
						fileTags.Add(tagItem);
					}
					DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, tagItem));
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error loading tags section.");
			}

			return Task.CompletedTask;
		}
	}
}
