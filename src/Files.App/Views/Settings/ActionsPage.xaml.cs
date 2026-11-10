// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Represents settings page called Actions, which provides a way to customize key bindings.
	/// </summary>
	public sealed partial class ActionsPage : Page
	{
		private ModifiableActionItem? _itemPendingDeletion;

		private ActionsViewModel ViewModel { get; set; } = new();

		public ActionsPage()
		{
			InitializeComponent();
			Loaded += (_, _) => ViewModel.StartListening();
			Unloaded += (_, _) => ViewModel.StopListening();
		}

		private void ActionsPage_Loaded(object sender, RoutedEventArgs e)
		{
			if (ViewModel.LoadAllActionsCommand.CanExecute(e))
				ViewModel.LoadAllActionsCommand.Execute(e);
		}

		private async void AddCommandButton_Click(SplitButton sender, SplitButtonClickEventArgs e)
		{
			await ShowKeyBindingEditorDialogAsync(null);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private async void EditButton_Click(object sender, RoutedEventArgs e)
		{
			// ItemsRepeater doesn't set DataContext on realized rows; the item comes through Tag
			if (sender is FrameworkElement { Tag: ModifiableActionItem item })
				await ShowKeyBindingEditorDialogAsync(item);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			// ItemsRepeater doesn't set DataContext on realized rows; the item comes through Tag
			if (sender is not FrameworkElement { Tag: ModifiableActionItem item } target)
				return;

			// Confirm before removing, anchored to the delete button
			_itemPendingDeletion = item;
			DeleteConfirmationTeachingTip.Target = target;
			DeleteConfirmationTeachingTip.IsOpen = true;
		}

		private void DeleteConfirmationTeachingTip_ActionButtonClick(TeachingTip sender, object args)
		{
			sender.IsOpen = false;

			if (_itemPendingDeletion is not null)
			{
				ViewModel.DeleteCommand.Execute(_itemPendingDeletion);
				_itemPendingDeletion = null;
			}
		}

		private async Task ShowKeyBindingEditorDialogAsync(ModifiableActionItem? itemToEdit)
		{
			var dialog = new KeyBindingEditorDialog(ViewModel, itemToEdit)
			{
				XamlRoot = XamlRoot,
			};

			await dialog.ShowAsync();
		}

		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var query = ((TextBox)sender).Text;
			ViewModel.FilterItems(query);
		}
	}
}
