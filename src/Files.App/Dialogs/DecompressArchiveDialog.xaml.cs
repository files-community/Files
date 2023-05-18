// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Files.Backend.SecureStore;
using Microsoft.UI.Xaml.Controls;
using System.Text;

namespace Files.App.Dialogs
{
	/// <summary>
	/// Represents an <see cref="ContentDialog"/> UI for archive decompression.
	/// </summary>
	public sealed partial class DecompressArchiveDialog : ContentDialog
	{
		public DecompressArchiveDialogViewModel ViewModel { get; set; }

		public DecompressArchiveDialog()
		{
			InitializeComponent();
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			// TODO: Do not manually invoke a command
			if (ViewModel.IsArchiveEncrypted)
				ViewModel.PrimaryButtonClickCommand.Execute(new DisposableArray(Encoding.UTF8.GetBytes(Password.Password)));
		}
	}
}
