using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.FileTags;
using Files.Uwp.Filesystem;
using System;

namespace Files.Uwp.UserControls.Menus
{
    public sealed partial class FileTagsContextMenu : UserControl
    {
        private IFileTagsSettingsService FileTagsSettingsService { get; } =
            Ioc.Default.GetService<IFileTagsSettingsService>();

        public FileTagsContextMenu()
        {
            InitializeComponent();
            ItemsSource = FileTagsSettingsService.FileTagList;
        }

        public IList<FileTagViewModel> ItemsSource { get; set; }

        public List<ListedItem> SelectedListedItems
        {
            get => (List<ListedItem>)GetValue(SelectedListedItemsProperty);
            set => SetValue(SelectedListedItemsProperty, value);
        }

        public static readonly DependencyProperty SelectedListedItemsProperty =
            DependencyProperty.Register("SelectedListedItems", typeof(List<ListedItem>), typeof(FileTagsContextMenu),
                new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
                {
                    var obj = s as FileTagsContextMenu;
                })));

        private void TagsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var added in e.AddedItems)
            {
                AddFileTag(SelectedListedItems, added as FileTagViewModel);
            }

            foreach (var removed in e.RemovedItems)
            {
                RemoveFileTag(SelectedListedItems, removed as FileTagViewModel);
            }
        }

        private static void RemoveFileTag(List<ListedItem> selectedListedItems, FileTagViewModel removed)
        {
            foreach (var selectedItem in selectedListedItems)
            {
                var existingTags = selectedItem.FileTags ?? Array.Empty<string>();
                if (existingTags.Contains(removed.Uid))
                {
                    selectedItem.FileTags = existingTags.Except(new[] { removed.Uid }).ToArray();
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

        private void TagsList_OnLoaded(object sender, RoutedEventArgs e)
        {
            // go through each tag and find the common one for all files
            var commonFileTags = SelectedListedItems
                .Select(x => x.FileTags ?? Enumerable.Empty<string>())
                .Aggregate((x, y) => x.Intersect(y))
                .Select(x => ItemsSource.FirstOrDefault(y => x == y.Uid));

            if (sender is ListView listView && listView.Items != null)
            {
                foreach (var commonTag in commonFileTags.Where(t => t != null))
                {
                    listView.SelectRange(new ItemIndexRange(listView.Items.IndexOf(commonTag), 1));
                }
            }
        }
    }
}