using Files.App.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Files.App.Dialogs
{
	public sealed partial class ReorderSidebarItemsDialog : ContentDialog, IDialog<ReorderSidebarItemsDialogViewModel>
	{
		public ReorderSidebarItemsDialogViewModel ViewModel
		{
			get => (ReorderSidebarItemsDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public ReorderSidebarItemsDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();
	}
}