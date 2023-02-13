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
			if (editingTag is not null)
			{
				editingTag.IsEditing = false;
				editingTag.NewColor = editingTag.Tag.Color;
			}

			editingTag = (ListedTagViewModel)((Button)sender).DataContext;
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
			var text = ((TextBox)sender).Text;
			editingTag.IsNameValid = IsNameValid(text);
		}

		private void CommitRenameTag_Click(object sender, RoutedEventArgs e)
		{
			var editingTag = (ListedTagViewModel)((Button)sender).DataContext;
			var item = TagsList.ContainerFromItem(editingTag) as ListViewItem;

			CommitChanges(item.FindDescendant("TagNameTextBox") as TextBox);
		}

		private void CancelRenameTag_Click(object sender, RoutedEventArgs e)
		{
			var editingTag = (ListedTagViewModel)((Button)sender).DataContext;
			var item = TagsList.ContainerFromItem(editingTag) as ListViewItem;
			editingTag.NewColor = editingTag.Tag.Color;

			EndEditing(item.FindDescendant("TagNameTextBox") as TextBox);
		}

		private void RemoveTag_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.DeleteExistingTag((ListedTagViewModel)((Button)sender).DataContext);
		}

		private void NewTagTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var text = ((TextBox)sender).Text;
			ViewModel.NewTag.Name = text;
			ViewModel.NewTag.IsNameValid = IsNameValid(text);
		}

		private bool IsNameValid(string name)
		{
			return !(string.IsNullOrWhiteSpace(name) || name.EndsWith('.') || name.StartsWith('.'));
		}
	}
}