// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using static Files.App.Helpers.MenuFlyoutHelper;

namespace Files.App.UserControls.Menus
{
	public sealed partial class FileTagsContextMenu : MenuFlyout
	{
		private IFileTagsSettingsService FileTagsSettingsService { get; } =
			Ioc.Default.GetService<IFileTagsSettingsService>();

		/// <summary>
		/// Event fired when an item's tags are updated (added/removed).
		/// Used to refresh groups in ShellViewModel.
		/// </summary>
		public event EventHandler? TagsChanged;

		private IEnumerable<ListedItem> selectedItems = [];
		public IEnumerable<ListedItem> SelectedItems => selectedItems;
		private Func<IEnumerable<ListedItem>>? selectedItemsProvider;

		public FileTagsContextMenu(IEnumerable<ListedItem> selectedItems)
		{
			this.selectedItems = selectedItems;
			Init();
		}

		/// <summary>
		/// Creates a menu with items provided dynamically via a delegate.
		/// Useful for reusing the menu instance across different selections.
		/// </summary>
		public FileTagsContextMenu(Func<IEnumerable<ListedItem>> selectedItemsProvider)
		{
			this.selectedItemsProvider = selectedItemsProvider;
			selectedItems = selectedItemsProvider?.Invoke() ?? [];
			Init();
		}

		private void Init()
		{
			SetValue(MenuFlyoutHelper.ItemsSourceProperty, FileTagsSettingsService.FileTagList
				.Select(tag => new MenuFlyoutFactoryItemViewModel(() =>
				{
					var tagItem = new ToggleMenuFlyoutItem()
					{
						Text = tag.Name,
						Tag = tag
					};
					tagItem.Icon = new PathIcon()
					{
						Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), (string)Application.Current.Resources["App.Theme.PathIcon.FilledTag"]),
						Foreground = new SolidColorBrush(ColorHelpers.FromHex(tag.Color))
					};
					tagItem.Click += TagItem_Click;
					return tagItem;
				})));

			Opening += Item_Opening;
		}

		/// <summary>
		/// Resets the flyout for a new selection so it can be reused across multiple opens.
		/// Clears all checked states and re-registers the opening handler.
		/// </summary>
		public void ResetForItems(IEnumerable<ListedItem> selectedItems)
		{
			this.selectedItems = selectedItems;
			foreach (var item in Items.OfType<ToggleMenuFlyoutItem>())
				item.IsChecked = false;
		}

		private void TagItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			var tagItem = (ToggleMenuFlyoutItem)sender;
			if (tagItem.IsChecked)
			{
				AddFileTag(SelectedItems, (TagViewModel)tagItem.Tag);
			}
			else
			{
				RemoveFileTag(SelectedItems, (TagViewModel)tagItem.Tag);
			}
		}

		private void Item_Opening(object? sender, object e)
		{
			// Update SelectedItems if using dynamic provider
			if (selectedItemsProvider is not null)
				selectedItems = selectedItemsProvider.Invoke();

			if (SelectedItems is null)
				return;

			foreach (var item in Items.OfType<ToggleMenuFlyoutItem>())
				item.IsChecked = false;

			// go through each tag and find the common one for all files
			var commonFileTags = SelectedItems
				.Select(x => x?.FileTags ?? Enumerable.Empty<string>())
				.DefaultIfEmpty(Enumerable.Empty<string>())
				.Aggregate((x, y) => x.Intersect(y))
				.Select(x => Items.FirstOrDefault(y => x == ((TagViewModel)y.Tag)?.Uid));

			commonFileTags.OfType<ToggleMenuFlyoutItem>().ForEach(x => x.IsChecked = true);
		}

		private void RemoveFileTag(IEnumerable<ListedItem> selectedListedItems, TagViewModel removed)
		{
			foreach (var selectedItem in selectedListedItems)
			{
				var existingTags = selectedItem.FileTags ?? [];
				if (existingTags.Contains(removed.Uid))
				{
					var tagList = existingTags.Except(new[] { removed.Uid }).ToArray();
					selectedItem.FileTags = tagList;
				}
			}
			TagsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void AddFileTag(IEnumerable<ListedItem> selectedListedItems, TagViewModel added)
		{
			foreach (var selectedItem in selectedListedItems)
			{
				var existingTags = selectedItem.FileTags ?? [];
				if (!existingTags.Contains(added.Uid))
				{
					selectedItem.FileTags = [.. existingTags, added.Uid];
				}
			}
			TagsChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
