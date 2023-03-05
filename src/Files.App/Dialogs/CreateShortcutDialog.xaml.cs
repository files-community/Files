using Files.App.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Files.App.Dialogs
{
	public sealed partial class CreateShortcutDialog : ContentDialog, IDialog<CreateShortcutViewModel>
	{
		public CreateShortcutViewModel ViewModel
		{
			get => (CreateShortcutViewModel)DataContext;
			set => DataContext = value;
		}

		public CreateShortcutDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();
	}
}