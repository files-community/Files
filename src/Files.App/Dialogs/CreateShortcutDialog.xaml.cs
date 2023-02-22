using Files.App.ViewModels.Dialogs;
using Files.Core.ViewModels.Dialogs;
using Files.Core.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Files.App.Dialogs
{
	public sealed partial class CreateShortcutDialog : ContentDialog, IDialog<CreateShortcutDialogViewModel>
	{
		public CreateShortcutDialogViewModel ViewModel
		{
			get => (CreateShortcutDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public CreateShortcutDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();
	}
}