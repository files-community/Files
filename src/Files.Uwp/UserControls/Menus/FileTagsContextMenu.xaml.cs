using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.FileTags;
using Files.Uwp.Filesystem;
using Files.Shared.Extensions;
using System;
using Windows.UI.Xaml.Media;
using Files.Backend.Models.Coloring;
using Files.Uwp.Helpers;

namespace Files.Uwp.UserControls.Menus
{
    public sealed partial class FileTagsContextMenu : MenuFlyout
    {
        private IFileTagsSettingsService FileTagsSettingsService { get; } =
            Ioc.Default.GetService<IFileTagsSettingsService>();

        public FileTagsContextMenu()
        {
            InitializeComponent();

            SetItemsSource();
        }

        private void SetItemsSource()
        {
            ItemsSource = FileTagsSettingsService.FileTagList.Select(x => new MenuFlyoutHelper.MenuFlyoutCustomItemViewModel(x, "FileTagTemplate"));
        }

        private IEnumerable<MenuFlyoutHelper.IMenuFlyoutItem> ItemsSource
        {
            get => (IEnumerable<MenuFlyoutHelper.IMenuFlyoutItem>)GetValue(MenuFlyoutHelper.ItemsSourceProperty);
            set => SetValue(MenuFlyoutHelper.ItemsSourceProperty, value);
        }

        public List<ListedItem> SelectedListedItems { get; set; }

        private void TagItem_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var tagItem = (ToggleMenuFlyoutItem)sender;
            if (tagItem.IsChecked)
            {
                AddFileTag(SelectedListedItems, (FileTagViewModel)tagItem.DataContext);
            }
            else
            {
                RemoveFileTag(SelectedListedItems, (FileTagViewModel)tagItem.DataContext);
            }
        }

        private static void RemoveFileTag(List<ListedItem> selectedListedItems, FileTagViewModel removed)
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

        private static void AddFileTag(List<ListedItem> selectedListedItems, FileTagViewModel added)
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

        private void MenuFlyout_Opening(object sender, object e)
        {
            // go through each tag and find the common one for all files
            var commonFileTags = SelectedListedItems
                .Select(x => x.FileTags ?? Enumerable.Empty<string>())
                .Aggregate((x, y) => x.Intersect(y))
                .Select(x => Items.FirstOrDefault(y => x == ((FileTagViewModel)y.DataContext)?.Uid));

            commonFileTags.OfType<ToggleMenuFlyoutItem>().ForEach(x => x.IsChecked = true);
        }
    }
}