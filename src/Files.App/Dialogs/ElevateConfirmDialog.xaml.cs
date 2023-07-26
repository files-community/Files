// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Files.App.Dialogs
{
	public sealed partial class ElevateConfirmDialog : ContentDialog, IDialog<ElevateConfirmDialogViewModel>
	{
		public ElevateConfirmDialogViewModel ViewModel
		{
			get => (ElevateConfirmDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public ElevateConfirmDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
		}
	}
}
