using Files.Core.ViewModels.Dialogs;
using Files.Core.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

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

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();
	}
}