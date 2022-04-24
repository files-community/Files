using Files.Uwp.DataModels.NavigationControlItems;
using Files.Backend.Services.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Files.Uwp.Filesystem
{
    public class FileTagsManager
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetService<IFileTagsSettingsService>();

        private readonly List<FileTagItem> fileTagList = new List<FileTagItem>();

        public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

        public IReadOnlyList<FileTagItem> FileTags
        {
            get
            {
                lock (fileTagList)
                {
                    return fileTagList.ToList().AsReadOnly();
                }
            }
        }

        public Task EnumerateFileTagsAsync()
        {
            if (!UserSettingsService.AppearanceSettingsService.ShowFileTagsSection)
            {
                return Task.CompletedTask;
            }

            try
            {
                foreach (var tag in FileTagsSettingsService.FileTagList)
                {
                    if (!fileTagList.Any(x => x.Path == $"tag:{tag.TagName}"))
                    {
                        var tagItem = new FileTagItem()
                        {
                            Text = tag.TagName,
                            Path = $"tag:{tag.TagName}",
                            FileTag = tag,
                            MenuOptions = new ContextMenuOptions
                            {
                                IsLocationItem = true
                            }
                        };
                        fileTagList.Add(tagItem);
                        DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, tagItem));
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, "Error loading tags section.");
            }

            return Task.CompletedTask;
        }
    }
}
