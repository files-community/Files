using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.FileTags;
using Files.Uwp.Filesystem;
using Files.Shared.Extensions;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Files.Uwp.Helpers;

namespace Files.Uwp.UserControls.Menus
{
    public class FileTagsContextMenu
    {
        private IFileTagsSettingsService FileTagsSettingsService { get; } =
            Ioc.Default.GetService<IFileTagsSettingsService>();

        public IEnumerable<ListedItem> SelectedItems { get; }

        public FileTagsContextMenu(IEnumerable<ListedItem> selectedItems)
        {
            SelectedItems = selectedItems;
        }

        public MenuFlyout Get()
        {
            var item = new MenuFlyout();
            item.Opening += Item_Opening;
            foreach (var tag in FileTagsSettingsService.FileTagList)
            {
                var tagItem = new ToggleMenuFlyoutItem()
                {
                    Text = tag.TagName,
                    Tag = tag
                };
                tagItem.Foreground = new SolidColorBrush(ColorHelpers.FromHex(tag.ColorString));
                tagItem.Click += TagItem_Click;
                item.Items.Add(tagItem);
            }
            return item;
        }

        private void TagItem_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var tagItem = (ToggleMenuFlyoutItem)sender;
            if (tagItem.IsChecked)
            {
                AddFileTag(SelectedItems, (FileTagViewModel)tagItem.Tag);
            }
            else
            {
                RemoveFileTag(SelectedItems, (FileTagViewModel)tagItem.Tag);
            }
        }

        private void Item_Opening(object sender, object e)
        {
            var item = (MenuFlyout)sender;
            item.Opening -= Item_Opening;

            // go through each tag and find the common one for all files
            var commonFileTags = SelectedItems
                .Select(x => x.FileTags ?? Enumerable.Empty<string>())
                .Aggregate((x, y) => x.Intersect(y))
                .Select(x => item.Items.FirstOrDefault(y => x == ((FileTagViewModel)y.Tag)?.Uid));

            commonFileTags.OfType<ToggleMenuFlyoutItem>().ForEach(x => x.IsChecked = true);
        }

        private void RemoveFileTag(IEnumerable<ListedItem> selectedListedItems, FileTagViewModel removed)
        {
            foreach (var selectedItem in selectedListedItems)
            {
                var existingTags = selectedItem.FileTags ?? Array.Empty<string>();
                if (existingTags.Contains(removed.Uid))
                {
                    var tagList = existingTags.Except(new[] { removed.Uid }).ToArray();
                    selectedItem.FileTags = tagList.Any() ? tagList : null;
                }
            }
        }

        private void AddFileTag(IEnumerable<ListedItem> selectedListedItems, FileTagViewModel added)
        {
            foreach (var selectedItem in selectedListedItems)
            {
                var existingTags = selectedItem.FileTags ?? Array.Empty<string>();
                if (!existingTags.Contains(added.Uid))
                {
                    selectedItem.FileTags = existingTags.Append(added.Uid).ToArray();
                }
            }
        }
    }
}
