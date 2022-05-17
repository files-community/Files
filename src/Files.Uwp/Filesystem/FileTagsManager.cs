using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Shared;
using Files.Uwp.DataModels.NavigationControlItems;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Uwp.Filesystem
{
    public class FileTagsManager
    {
        private readonly ILogger logger = Ioc.Default.GetService<ILogger>();
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

        public Task UpdateFileTagsAsync()
        {
            try
            {
                foreach (var tag in fileTagsSettingsService.FileTagList)
                {
                    var tagItem = new FileTagItem
                    {
                        Text = tag.TagName,
                        Path = $"tag:{tag.TagName}",
                        FileTag = tag,
                        MenuOptions = new ContextMenuOptions{ IsLocationItem = true },
                    };

                    lock (fileTags)
                    {
                        if (fileTags.Any(x => x.Path == $"tag:{tag.TagName}"))
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
                logger.Warn(ex, "Error loading tags section.");
            }

            return Task.CompletedTask;
        }
    }
}
