using System;
using System.Threading.Tasks;
using Files.Backend.ViewModels.Dialogs;
using Files.Backend.ViewModels.Dialogs.AddItemDialog;
using Files.Extensions;
using Files.Shared.Enums;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Dialogs
{
	public sealed partial class AddItemDialog : ContentDialog, IDialog<AddItemDialogViewModel>
	{
		public AddItemDialogViewModel ViewModel
		{
			get => (AddItemDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public AddItemDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

		private void ListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			ViewModel.ResultType = (e.ClickedItem as AddItemDialogListItemViewModel).ItemResult;
			this.Hide();
		}

		private async void AddItemDialog_Loaded(object sender, RoutedEventArgs e)
		{
			var itemTypes = await ShellNewEntryExtensions.GetNewContextMenuEntries();
			await ViewModel.AddItemsToList(itemTypes); // TODO(i): This is a very cheap way of doing it, consider adding a service to retrieve the itemTypes list.
		}
	}
}