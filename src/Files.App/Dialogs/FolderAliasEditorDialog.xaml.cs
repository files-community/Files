// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class FolderAliasEditorDialog : ContentDialog
	{
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();

		private readonly FolderAliasesViewModel _viewModel;
		private readonly FolderAliasItem? _editingItem;

		private string AliasName
			=> NameTextBox.Text.Trim().TrimStart(FolderAliasHelpers.Prefix);

		private string AliasPath
			=> PathTextBox.Text.Trim().Trim('"');

		public FolderAliasEditorDialog(FolderAliasesViewModel viewModel, FolderAliasItem? itemToEdit = null)
		{
			_viewModel = viewModel;
			_editingItem = itemToEdit;

			InitializeComponent();

			Title = itemToEdit is null ? Strings.AddAlias.GetLocalizedResource() : Strings.Edit.GetLocalizedResource();
			PrimaryButtonText = itemToEdit is null ? Strings.Add.GetLocalizedResource() : Strings.Save.GetLocalizedResource();

			NameTextBox.Text = itemToEdit?.Name ?? string.Empty;
			PathTextBox.Text = itemToEdit?.Path ?? string.Empty;
			Validate();

			PrimaryButtonClick += FolderAliasEditorDialog_PrimaryButtonClick;
		}

		private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			Validate();
		}

		private void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
			if (CommonDialogService.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, true, [], Environment.SpecialFolder.Desktop, out var folderPath))
				PathTextBox.Text = folderPath;
		}

		private void Validate()
		{
			var name = AliasName;
			var error =
				name.Length is 0 ? null :
				!FolderAliasHelpers.IsValidName(name) ? Strings.FolderAliasNameInvalid.GetLocalizedResource() :
				_viewModel.IsNameInUse(name, _editingItem) ? Strings.FolderAliasNameInUse.GetLocalizedResource() :
				null;

			NameErrorTextBlock.Text = error ?? string.Empty;
			NameErrorTextBlock.Visibility = error is null ? Visibility.Collapsed : Visibility.Visible;
			IsPrimaryButtonEnabled = error is null && name.Length > 0 && AliasPath.Length > 0;
		}

		private void FolderAliasEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			var newItem = new FolderAliasItem(AliasName, AliasPath);
			if (_editingItem is null)
				_viewModel.Add(newItem);
			else
				_viewModel.Replace(_editingItem, newItem);
		}
	}
}
