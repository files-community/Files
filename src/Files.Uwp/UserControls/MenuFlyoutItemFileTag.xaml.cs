﻿using Files.Uwp.Filesystem;
using Files.Backend.Services.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Backend.ViewModels.FileTags;

namespace Files.Uwp.UserControls
{
    public sealed partial class MenuFlyoutItemFileTag : UserControl
    {
        private IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetService<IFileTagsSettingsService>();

        public List<ListedItem> SelectedItems
        {
            get { return (List<ListedItem>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(List<ListedItem>), typeof(MenuFlyoutItemFileTag), new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
            {
                var obj = s as MenuFlyoutItemFileTag;
                obj.SelectedItem = obj.GetFileTag(e.NewValue as List<ListedItem>);
            })));

        public FileTagViewModel SelectedItem
        {
            get => (FileTagViewModel)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(FileTagViewModel), typeof(MenuFlyoutItemFileTag), new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
            {
                var obj = s as MenuFlyoutItemFileTag;
                obj.SetFileTag(obj.SelectedItems, e.NewValue as FileTagViewModel);
            })));

        public IList<FileTagViewModel> ItemsSource
        {
            get { return (IList<FileTagViewModel>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IList<FileTagViewModel>), typeof(MenuFlyoutItemFileTag), new PropertyMetadata(null));

        public MenuFlyoutItemFileTag()
        {
            this.InitializeComponent();
        }

        private async void TagList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var listView = sender as ListView;
            if (e.ClickedItem == listView.SelectedItem)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => listView.SelectedItem = null);
            }
        }

        public FileTagViewModel GetFileTag(List<ListedItem> selectedItems)
        {
            if (selectedItems == null || selectedItems.Count == 0)
            {
                return null;
            }
            else if (selectedItems.Count == 1)
            {
                return FileTagsSettingsService.GetTagById(selectedItems.First().FileTag);
            }
            else
            {
                var tag = selectedItems.First().FileTag;
                return selectedItems.All(x => x.FileTag == tag) ? FileTagsSettingsService.GetTagById(tag) : null;
            }
        }

        public void SetFileTag(List<ListedItem> selectedItems, FileTagViewModel selectedTag)
        {
            if (selectedItems == null || selectedItems.Count == 0)
            {
                return;
            }
            else if (selectedItems.Count == 1)
            {
                selectedItems.First().FileTag = selectedTag?.Uid;
            }
            else
            {
                var tag = selectedItems.First().FileTag;
                if (selectedTag != null || selectedItems.All(x => x.FileTag == tag))
                {
                    foreach (var item in selectedItems)
                    {
                        item.FileTag = selectedTag?.Uid;
                    }
                }
            }
        }
    }
}