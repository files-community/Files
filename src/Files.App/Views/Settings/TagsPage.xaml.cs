// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using WinRT;

namespace Files.App.Views.Settings
{
	public sealed partial class TagsPage : Page
	{
		private readonly IWindowContext WindowContext = Ioc.Default.GetRequiredService<IWindowContext>();
		private readonly IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

		// Will be null unless the user has edited any tag
		private ListedTagViewModel? editingTag;

		private FlyoutBase? deleteItemFlyout;

		public bool AllowItemsDrag
			=> WindowContext.CanDragAndDrop;

		public TagsPage()
		{
			InitializeComponent();
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			fileTagsSettingsService.OnTagsUpdated += OnTagsUpdated;
			RefreshTags();
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			fileTagsSettingsService.OnTagsUpdated -= OnTagsUpdated;
		}

		private void OnTagsUpdated(object? sender, EventArgs e)
		{
			// Reorder saves originate inside CollectionChanged; refresh after that event returns.
			DispatcherQueue.TryEnqueue(() =>
			{
				if (IsLoaded)
					RefreshTags();
			});
		}

		private void RefreshTags()
		{
			var tags = fileTagsSettingsService.FileTagList;
			if (editingTag is not null && !tags.Any(tag => tag.Uid == editingTag.Tag.Uid))
				CloseEdit();
			ViewModel.UpdateTags(tags);
		}

		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			var textBox = (TextBox)sender;
			switch (e.Key)
			{
				case VirtualKey.Enter:
					if (!editingTag!.CanCommit)
						return;

					CommitChanges(textBox);
					e.Handled = true;
					break;
			}
		}

		[DynamicWindowsRuntimeCast(typeof(Button))]
		private void EditTag_Click(object sender, RoutedEventArgs e)
		{
			CloseEdit();

			if (sender is not Button { DataContext: ListedTagViewModel tag })
				return;

			editingTag = tag;
			editingTag.NewColor = editingTag.Tag.Color;
			editingTag.NewName = editingTag.Tag.Name;
			editingTag.IsEditing = true;
		}

		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private void CommitRenameTag_Click(object sender, RoutedEventArgs e)
		{
			var item = (TagsList.ContainerFromItem(editingTag) as ListViewItem)!;

			CommitChanges((item.FindDescendant("TagNameTextBox") as TextBox)!);
		}

		private void CancelRenameTag_Click(object sender, RoutedEventArgs e)
		{
			CloseEdit();
		}

		[DynamicWindowsRuntimeCast(typeof(Button))]
		private void PreRemoveTag_Click(object sender, RoutedEventArgs e)
		{
			deleteItemFlyout = ((Button)sender).Flyout;
		}

		private void CancelRemoveTag_Click(object sender, RoutedEventArgs e)
		{
			deleteItemFlyout?.Hide();
		}

		[DynamicWindowsRuntimeCast(typeof(Button))]
		private void RemoveTag_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.DeleteExistingTag((ListedTagViewModel)((Button)sender).DataContext);
		}

		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private void RenameTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var textBox = (TextBox)sender;
			if (editingTag is not { } tag || !ReferenceEquals(textBox.DataContext, tag))
				return;
			var text = textBox.Text;
			var isNullOrEmpty = string.IsNullOrEmpty(text);
			tag.IsNameValid = isNullOrEmpty || (IsNameValid(text) && !ViewModel.Tags.Any(item => item.Tag.Name == text && tag.Tag.Name != text));
			tag.CanCommit = !isNullOrEmpty && tag.IsNameValid && (
				text != tag.Tag.Name ||
				tag.NewColor != tag.Tag.Color
			);
		}

		private void EditColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
		{
			if (editingTag is null || !ReferenceEquals(sender.DataContext, editingTag))
				return;

			editingTag.CanCommit = editingTag.IsNameValid && (
				editingTag.NewName != editingTag.Tag.Name ||
				CommunityToolkit.WinUI.Helpers.ColorHelper.ToHex(sender.Color) != editingTag.Tag.Color
			);
		}

		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private void NewTagTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var text = ((TextBox)sender).Text;
			ViewModel.NewTag.Name = text;
			ViewModel.NewTag.IsNameValid = string.IsNullOrEmpty(text) || (IsNameValid(text) && !ViewModel.Tags.Any(tag => text == tag.Tag.Name));
		}

		private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (args.KeyboardAccelerator.Key is VirtualKey.Escape && editingTag is not null)
			{
				CloseEdit();
				args.Handled = true;
			}
		}

		private void CommitChanges(TextBox textBox)
		{
			var tag = editingTag!;
			EndEditing();
			string newTagName = textBox.Text.Trim().TrimEnd('.');
			if (newTagName != tag.Tag.Name || tag.NewColor != tag.Tag.Color)
				ViewModel.EditExistingTag(tag, newTagName, tag.NewColor);
		}

		private void EndEditing()
		{
			editingTag!.IsEditing = false;
			editingTag = null;
		}

		private void CloseEdit()
		{
			if (editingTag is not { } tag)
				return;
			tag.NewColor = tag.Tag.Color;
			tag.IsNameValid = true;
			tag.CanCommit = false;

			EndEditing();
		}

		private bool IsNameValid(string name)
		{
			return !(
				string.IsNullOrWhiteSpace(name) ||
				name.StartsWith('.') ||
				name.EndsWith('.')
			);
		}
	}
}
