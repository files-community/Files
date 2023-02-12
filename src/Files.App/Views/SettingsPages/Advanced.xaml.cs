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
				case VirtualKey.Up:
					textBox.SelectionStart = 0;
					e.Handled = true;
					break;
				case VirtualKey.Down:
					textBox.SelectionStart = textBox.Text.Length;
					e.Handled = true;
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
			if (textBox is not null && textBox.FindParent<Grid>() is FrameworkElement parent)
				Grid.SetColumnSpan(parent, 1);

			ListViewItem? listViewItem = TagsList.ContainerFromItem(renamingTag) as ListViewItem;

			textBox!.LostFocus -= RenameTextBox_LostFocus;
			textBox.KeyDown -= RenameTextBox_KeyDown;

			renamingTag.IsRenaming = false;
		}

		private void RenameTag_Click(object sender, RoutedEventArgs e)
		{
			renamingTag = (ListedTagViewModel)(sender as MenuFlyoutItem).DataContext;
			renamingTag.IsRenaming = true;

			var item = TagsList.ContainerFromItem(renamingTag) as ListViewItem;
			var textBlock = item.FindDescendant("TagName") as TextBlock;
			var textBox = item.FindDescendant("TagNameTextBox") as TextBox;

			textBox!.Text = textBlock!.Text;
			oldTagName = textBlock.Text;

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
			ViewModel.DeleteExistingTag((ListedTagViewModel)(sender as MenuFlyoutItem).DataContext);
		}
	}
}