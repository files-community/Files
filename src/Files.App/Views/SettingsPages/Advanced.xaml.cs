using CommunityToolkit.WinUI.UI;
using Files.Backend.ViewModels.FileTags;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Files.App.SettingsPages
{
	public sealed partial class Advanced : Page
	{
		private string oldTagName = string.Empty;

		private ListedTagViewModel? editingTag;

		public Advanced()
		{
			InitializeComponent();
		}

		private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			var textBox = (TextBox)sender;
			switch (e.Key)
			{
				case VirtualKey.Enter:
					CommitChanges(textBox);
					e.Handled = true;
					break;
			}
		}

		private void CommitChanges(TextBox textBox)
		{
			EndEditing(textBox);
			string newTagName = textBox.Text.Trim().TrimEnd('.');
			if (newTagName != oldTagName || editingTag.NewColor != editingTag.Tag.Color)
				ViewModel.EditExistingTag(editingTag, newTagName, editingTag.NewColor);
		}

		private void EndEditing(TextBox? textBox)
		{
			textBox.TextChanged -= RenameTextBox_TextChanged;
			editingTag.IsEditing = false;
		}

		private void EditTag_Click(object sender, RoutedEventArgs e)
		{
			editingTag = (ListedTagViewModel)(sender as Button).DataContext;
			editingTag.NewColor = editingTag.Tag.Color;
			editingTag.IsEditing = true;

			var item = TagsList.ContainerFromItem(editingTag) as ListViewItem;
			var textBlock = item.FindDescendant("TagName") as TextBlock;
			var textBox = item.FindDescendant("TagNameTextBox") as TextBox;

			textBox.TextChanged += RenameTextBox_TextChanged;

			textBox!.Text = textBlock!.Text;
			oldTagName = textBlock.Text;
		}

		private void RenameTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var text = (sender as TextBox).Text;
			editingTag.IsNameValid = !(string.IsNullOrWhiteSpace(text) || text.EndsWith('.') || text.StartsWith('.'));
		}

		private void CommitRenameTag_Click(object sender, RoutedEventArgs e)
		{
			var item = TagsList.ContainerFromItem(editingTag) as ListViewItem;
			CommitChanges(item.FindDescendant("TagNameTextBox") as TextBox);
		}

		private void RemoveTag_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.DeleteExistingTag((ListedTagViewModel)(sender as Button).DataContext);
		}
	}
}