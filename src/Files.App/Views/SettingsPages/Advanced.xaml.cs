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

		private ListedTagViewModel? renamingTag;

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
					CommitRename(textBox);
					e.Handled = true;
					break;
			}
		}

		private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
		{
			if (sender.DataContext is not ListedTagViewModel item || args.NewColor.ToString() == item.Tag.Color)
				return;

			ViewModel.EditExistingTag(item, item.Tag.Name, args.NewColor.ToString());
		}

		private void CommitRename(TextBox textBox)
		{
			EndRename(textBox);
			string newTagName = textBox.Text.Trim().TrimEnd('.');
			if (newTagName != oldTagName)
				ViewModel.EditExistingTag(renamingTag, newTagName, renamingTag.Tag.Color);
		}

		private void EndRename(TextBox? textBox)
		{
			renamingTag.IsEditing = false;
		}

		private void EditTag_Click(object sender, RoutedEventArgs e)
		{
			renamingTag = (ListedTagViewModel)(sender as Button).DataContext;
			renamingTag.IsEditing = true;

			var item = TagsList.ContainerFromItem(renamingTag) as ListViewItem;
			var textBlock = item.FindDescendant("TagName") as TextBlock;
			var textBox = item.FindDescendant("TagNameTextBox") as TextBox;

			textBox!.Text = textBlock!.Text;
			oldTagName = textBlock.Text;
		}

		private void CommitRenameTag_Click(object sender, RoutedEventArgs e)
		{
			var item = TagsList.ContainerFromItem(renamingTag) as ListViewItem;
			CommitRename(item.FindDescendant("TagNameTextBox") as TextBox);
		}

		private void RemoveTag_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.DeleteExistingTag((ListedTagViewModel)(sender as Button).DataContext);
		}
	}
}