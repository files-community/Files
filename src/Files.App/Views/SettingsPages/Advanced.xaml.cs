using CommunityToolkit.WinUI.UI;
using Files.Backend.ViewModels.FileTags;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Files.App.SettingsPages
{
	public sealed partial class Advanced : Page
	{
		private string oldTagName = string.Empty;

		private TagViewModel? renamingTag;

		public Advanced()
		{
			InitializeComponent();
		}

		private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			var textBox = (TextBox)sender;
			switch (e.Key)
			{
				case VirtualKey.Escape:
					textBox.LostFocus -= RenameTextBox_LostFocus;
					textBox.Text = oldTagName;
					EndRename(textBox);
					e.Handled = true;
					break;
				case VirtualKey.Enter:
					textBox.LostFocus -= RenameTextBox_LostFocus;
					CommitRename(textBox);
					e.Handled = true;
					break;
				case VirtualKey.Up:
					textBox.SelectionStart = 0;
					e.Handled = true;
					break;
				case VirtualKey.Down:
					textBox.SelectionStart = textBox.Text.Length;
					e.Handled = true;
					break;
				case VirtualKey.Left:
					e.Handled = textBox.SelectionStart == 0;
					break;
				case VirtualKey.Right:
					e.Handled = (textBox.SelectionStart + textBox.SelectionLength) == textBox.Text.Length;
					break;
			}
		}

		private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (!(FocusManager.GetFocusedElement(XamlRoot) is AppBarButton or Popup))
			{
				var textBox = (TextBox)e.OriginalSource;
				CommitRename(textBox);
			}
		}

		private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
		{
			if (sender.DataContext is not TagViewModel tag || args.NewColor.ToString() == tag.Color)
				return;
			ViewModel.EditExistingTag(tag, tag.Name, args.NewColor.ToString());
		}

		private void CommitRename(TextBox textBox)
		{
			EndRename(textBox);
			string newTagName = textBox.Text.Trim().TrimEnd('.');
			if (newTagName != oldTagName)
				ViewModel.EditExistingTag(renamingTag, newTagName, renamingTag.Color);
		}

		private void EndRename(TextBox? textBox)
		{
			if (textBox is not null && textBox.FindParent<Grid>() is FrameworkElement parent)
				Grid.SetColumnSpan(parent, 1);

			ListViewItem? listViewItem = TagsList.ContainerFromItem(renamingTag) as ListViewItem;

			if (textBox is not null && listViewItem is not null)
			{
				var textBlock = listViewItem.FindDescendant("TagName") as TextBlock;
				var editButton = listViewItem.FindDescendant("RenameButton") as Button;
				var commitButton = listViewItem.FindDescendant("CommitRenameButton") as Button;
				textBox.Visibility = Visibility.Collapsed;
				commitButton.Visibility = Visibility.Collapsed;
				textBlock.Visibility = Visibility.Visible;
				editButton.Visibility = Visibility.Visible;
			}

			textBox!.LostFocus -= RenameTextBox_LostFocus;
			textBox.KeyDown -= RenameTextBox_KeyDown;

			// Re-focus selected list item
			listViewItem?.Focus(FocusState.Programmatic);
		}

		private void RenameTag_Click(object sender, RoutedEventArgs e)
		{
			renamingTag = (TagViewModel)(sender as MenuFlyoutItem).DataContext;

			var item = TagsList.ContainerFromItem(renamingTag) as ListViewItem;
			var textBlock = item.FindDescendant("TagName") as TextBlock;
			var textBox = item.FindDescendant("TagNameTextBox") as TextBox;
			var editButton = item.FindDescendant("RenameButton") as Button;
			var commitButton = item.FindDescendant("CommitRenameButton") as Button;

			textBox!.Text = textBlock!.Text;
			oldTagName = textBlock.Text;
			textBlock.Visibility = Visibility.Collapsed;
			editButton.Visibility = Visibility.Collapsed;
			textBox.Visibility = Visibility.Visible;
			commitButton.Visibility = Visibility.Visible;

			Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);

			textBox.Focus(FocusState.Pointer);
			textBox.LostFocus += RenameTextBox_LostFocus;
			textBox.KeyDown += RenameTextBox_KeyDown;
		}

		private void CommitRenameTag_Click(object sender, RoutedEventArgs e)
		{
			var item = TagsList.ContainerFromItem(renamingTag) as ListViewItem;
			CommitRename(item.FindDescendant("TagNameTextBox") as TextBox);
		}

		private void RemoveTag_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.DeleteExistingTag((TagViewModel)(sender as Button).DataContext);
		}
	}
}