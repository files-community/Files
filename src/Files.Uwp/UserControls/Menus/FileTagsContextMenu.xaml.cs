using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.FileTags;
using Files.Uwp.Filesystem;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

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
                var existingTags = selectedItem.FileTags.ToList();
                if (existingTags.Contains(removed.Uid))
                {
                    existingTags.Remove(removed.Uid);
                    selectedItem.FileTags = existingTags.ToArray();
                }
            }
        }

        private static void AddFileTag(List<ListedItem> selectedListedItems, FileTagViewModel added)
        {
            foreach (var selectedItem in selectedListedItems)
            {
                var existingTags = selectedItem.FileTags.ToList();
                if (!existingTags.Contains(added.Uid))
                {
                    existingTags.Add(added.Uid);
                    selectedItem.FileTags = existingTags.ToArray();
                }
            }
        }

        private void TagsList_OnLoaded(object sender, RoutedEventArgs e)
        {
            //go through each tag and find the common one for all files
            var commonFileTags = (from fileTagViewModel in ItemsSource
                let tagFileCount =
                    SelectedListedItems.Count(selectedListedItem =>
                        selectedListedItem.FileTags.Contains(fileTagViewModel.Uid))
                where tagFileCount == SelectedListedItems.Count
                select fileTagViewModel).ToList();

            var listView = sender as ListView;
            
            foreach (var commonTag in commonFileTags.Where(_ => listView?.Items != null))
            {
                listView?.SelectRange(new ItemIndexRange(listView.Items.IndexOf(commonTag), 1));
            }
        }
    }
}