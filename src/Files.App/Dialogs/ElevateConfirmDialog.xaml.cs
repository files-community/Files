// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class ElevateConfirmDialog : ContentDialog, IDialog<ElevateConfirmDialogViewModel>
	{
		public ElevateConfirmDialogViewModel ViewModel { get; set; }

		public ElevateConfirmDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return await base.ShowAsync();
		}
	}
}
