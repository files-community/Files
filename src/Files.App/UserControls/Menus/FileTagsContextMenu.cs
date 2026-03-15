// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Files.App.ViewModels.UserControls.Menus;

namespace Files.App.UserControls.Menus
{
	public sealed partial class FileTagsContextMenu : MenuFlyout
	{
		private readonly MenuFlyoutItem removeAllTagsMenuItem;

		private readonly FileTagsContextMenuViewModel viewModel;

		/// <summary>
		/// Event fired when an item's tags are updated (added/removed).
		/// Used to refresh groups in ShellViewModel.
		/// </summary>
		public event EventHandler? TagsChanged;

		public IEnumerable<ListedItem> SelectedItems
			=> viewModel.SelectedItems;

		public FileTagsContextMenu(IEnumerable<ListedItem> selectedItems)
		{
			viewModel = new(
				selectedItems,
				Ioc.Default.GetRequiredService<IFileTagsSettingsService>());
			viewModel.TagsChanged += ViewModel_TagsChanged;

			removeAllTagsMenuItem = new MenuFlyoutItem()
			{
				Text = Strings.RemoveAllTags.GetLocalizedResource(),
				Command = viewModel.RemoveAllTagsCommand,
				Icon = new PathIcon()
				{
					Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), (string)Application.Current.Resources["App.Theme.PathIcon.ActionDelete"]),
				}
			};
			AutomationProperties.SetName(removeAllTagsMenuItem, Strings.RemoveAllTags.GetLocalizedResource());

			Items.Add(removeAllTagsMenuItem);
			Items.Add(new MenuFlyoutSeparator());

			foreach (var tag in viewModel.AvailableTags)
			{
				Items.Add(CreateTagItem(tag));
			}

			Opening += Item_Opening;
		}

		private ToggleMenuFlyoutItem CreateTagItem(TagViewModel tag)
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
			AutomationProperties.SetName(tagItem, tag.Name);
			tagItem.Click += TagItem_Click;

			return tagItem;
		}

		private void TagItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			var tagItem = (ToggleMenuFlyoutItem)sender;
			viewModel.UpdateTagSelection((TagViewModel)tagItem.Tag, tagItem.IsChecked);
			removeAllTagsMenuItem.IsEnabled = viewModel.CanRemoveAllTags;
		}

		private void Item_Opening(object? sender, object e)
		{
			removeAllTagsMenuItem.IsEnabled = viewModel.CanRemoveAllTags;

			Items
				.OfType<ToggleMenuFlyoutItem>()
				.ForEach(tagItem => tagItem.IsChecked = viewModel.IsTagAppliedToAllSelectedItems((TagViewModel)tagItem.Tag));
		}

		private void ViewModel_TagsChanged(object? sender, EventArgs e)
		{
			TagsChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
