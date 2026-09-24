// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Dialogs;
using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinRT;

namespace Files.App.Views.Settings
{
	public sealed partial class FoldersPage : Page
	{
		private FolderAliasItem? _aliasPendingDeletion;

		private FolderAliasesViewModel AliasesViewModel { get; } = new();

		public FoldersPage()
		{
			InitializeComponent();
		}

		[DynamicWindowsRuntimeCast(typeof(Control))]
		private void AliasRow_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((Control)sender, "PointerOver", false);
		}

		[DynamicWindowsRuntimeCast(typeof(Control))]
		private void AliasRow_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((Control)sender, "Normal", false);
		}

		private async void AddAliasButton_Click(SplitButton sender, SplitButtonClickEventArgs e)
		{
			await ShowAliasEditorDialogAsync(null);
		}

		private void RestoreDefaultAliasesMenuItem_Click(object sender, RoutedEventArgs e)
		{
			RestoreDefaultAliasesTeachingTip.IsOpen = true;
		}

		private void RestoreDefaultAliasesTeachingTip_ActionButtonClick(TeachingTip sender, object args)
		{
			sender.IsOpen = false;
			AliasesViewModel.RestoreDefaults();
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private async void EditAliasButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement { Tag: FolderAliasItem item })
				await ShowAliasEditorDialogAsync(item);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void DeleteAliasButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not FrameworkElement { Tag: FolderAliasItem item } target)
				return;

			_aliasPendingDeletion = item;
			DeleteAliasConfirmationTeachingTip.Target = target;
			DeleteAliasConfirmationTeachingTip.IsOpen = true;
		}

		private void DeleteAliasConfirmationTeachingTip_ActionButtonClick(TeachingTip sender, object args)
		{
			sender.IsOpen = false;

			if (_aliasPendingDeletion is not null)
			{
				AliasesViewModel.Remove(_aliasPendingDeletion);
				_aliasPendingDeletion = null;
			}
		}

		private async Task ShowAliasEditorDialogAsync(FolderAliasItem? itemToEdit)
		{
			var dialog = new FolderAliasEditorDialog(AliasesViewModel, itemToEdit)
			{
				XamlRoot = XamlRoot,
			};

			await dialog.TryShowAsync();
		}
	}
}
