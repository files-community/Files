using CommunityToolkit.WinUI.UI;
using Files.Backend.ViewModels.FileTags;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.InteropServices;
using Windows.System;

namespace Files.App.SettingsPages
{
	public sealed partial class Advanced : Page
	{
		private const int KEY_DOWN_MASK = 0x8000;

		private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer tapDebounceTimer;

		private string oldTagName = string.Empty;

		private int nextRenameIndex = 0;

		private TagViewModel? renamingTag;

		private TagViewModel? preRenamingTag;

		public Advanced()
		{
			InitializeComponent();

			tapDebounceTimer = DispatcherQueue.CreateTimer();
		}

		public void StartRenameTag()
		{
			renamingTag = (TagViewModel)TagsList.SelectedItem;
			var item = TagsList.ContainerFromItem(TagsList.SelectedItem) as ListViewItem;
			var textBlock = item.FindDescendant("TagName") as TextBlock;
			var textBox = item.FindDescendant("TagNameTextBox") as TextBox;

			textBox!.Text = textBlock!.Text;
			oldTagName = textBlock.Text;
			textBlock.Visibility = Visibility.Collapsed;
			textBox.Visibility = Visibility.Visible;

			Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);

			textBox.Focus(FocusState.Pointer);
			textBox.LostFocus += RenameTextBox_LostFocus;
			textBox.KeyDown += RenameTextBox_KeyDown;
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
				case VirtualKey.Tab:
					textBox.LostFocus -= RenameTextBox_LostFocus;

					var isShiftPressed = (GetKeyState((int)VirtualKey.Shift) & KEY_DOWN_MASK) != 0;
					nextRenameIndex = isShiftPressed ? -1 : 1;

					var newIndex = TagsList.SelectedIndex + nextRenameIndex;
					nextRenameIndex = 0;

					if (textBox.Text != oldTagName)
						CommitRename(textBox);
					else
						EndRename(textBox);

					if
					(
						newIndex >= 0 &&
						newIndex < TagsList.Items.Count
					)
					{
						TagsList.SelectedIndex = newIndex;
						StartRenameTag();
					}

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

		private void EndRename(TextBox textBox)
		{
			if (textBox is not null && textBox.FindParent<Grid>() is FrameworkElement parent)
				Grid.SetColumnSpan(parent, 1);

			ListViewItem? listViewItem = TagsList.ContainerFromItem(renamingTag) as ListViewItem;

			if (textBox is not null && listViewItem is not null)
			{
				var textBlock = listViewItem.FindDescendant("TagName") as TextBlock;
				textBox.Visibility = Visibility.Collapsed;
				textBlock!.Visibility = Visibility.Visible;
			}

			textBox!.LostFocus -= RenameTextBox_LostFocus;
			textBox.KeyDown -= RenameTextBox_KeyDown;

			// Re-focus selected list item
			listViewItem?.Focus(FocusState.Programmatic);
		}

		private void TagsList_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var clickedItem = e.OriginalSource as FrameworkElement;
			if (clickedItem is TextBlock textBox && textBox.Name == "TagName")
			{
				if (clickedItem.DataContext is TagViewModel tag)
				{
					if (tag == preRenamingTag)
					{
						tapDebounceTimer.Debounce(() =>
						{
							if (tag == preRenamingTag)
							{
								StartRenameTag();
								tapDebounceTimer.Stop();
							}
						}, TimeSpan.FromMilliseconds(500));
					}
					else
					{
						tapDebounceTimer.Stop();
						preRenamingTag = tag;
					}
				}
				else
				{
					ResetRenameDoubleClick();
				}
			}
		}

		private void ResetRenameDoubleClick()
		{
			preRenamingTag = null;
			tapDebounceTimer.Stop();
		}

		[DllImport("User32.dll")]
		private extern static short GetKeyState(int n);
	}
}