using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

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
			=> (DialogResult)await base.ShowAsync();
	}
}
