// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;
using System.Text;

namespace Files.App.Dialogs
{
	public sealed partial class DecompressArchiveDialog : ContentDialog
	{
		public DecompressArchiveDialogViewModel ViewModel
		{
			get => (DecompressArchiveDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public DecompressArchiveDialog()
		{
			InitializeComponent();
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			if (ViewModel.IsArchiveEncrypted)
				ViewModel.PrimaryButtonClickCommand.Execute(new DisposableArray(Encoding.UTF8.GetBytes(Password.Password)));
		}
	}
}
